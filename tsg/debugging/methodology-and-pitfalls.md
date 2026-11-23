# 取证与避坑：根因定位方法论与高频认知陷阱

## 1. 适用问题（Questions Answered）

- 报表里的 `Send`、`Receive`、`ExpectedRecievedMessageCount` 究竟在哪一行代码累加？“少发了”和“发了没收到”怎么区分？
- 看到少量断连重连或一个代码瑕疵时，怎么判断它是不是本次几万条消息缺口的主因？
- 当延迟突然翻倍甚至暴涨 10 倍时，如何在多容器/共享节点架构下准确拆分“是主进程变慢了”还是“被旁路容器抢核限流了”？为什么只对比“上次绿灯 vs 本次黄灯”会漏掉真正引入退化的历史变更？
- 手动重跑、判读黄灯恢复和修改状态持久化时有哪些容易踩的坑？

---

## 2. 标准取证动作（Standard Probes）

### 2.1 第一步：画清计数确认边界

在怀疑“丢消息”前，先对照 [`ClientAgentBehaviorSettings.cs`](../../src/Pods/Client/ClientAgentBehaviorSettings.cs) 与对应 `IClientAgent` 确认分母 `ExpectedRecievedMessageCount` 的产生时机：

```text
发送循环调用 Agent 发送方法
  ├─ 抛异常 ──────────────► 记录 Warning 日志，不计入 Sent，也不计入 ExpectedReceived
  └─ 正常返回（未抛异常） ─► Sent += 1，ExpectedReceived += 预期接收人数（P2P/Echo=1，Group=GroupSize，Broadcast=TotalConn）
```

- **推论 1（排除“发得慢导致比例低”）**：因为 `ExpectedReceived` 跟随实际成功的发送调用累加，而非固定取 `Active * Duration`，单纯“发送循环慢、少发了几百条”只会等比例减少 `Sent` 和 `ExpectedReceived`，**绝不会导致 `Received / ExpectedReceived < 95%`**。
- **推论 2（检查确认链是否提前截断）**：对经由 HTTP 转发到 Upstream / AppServer 的场景（如 WPS/MQTT 的 P2P、Broadcast），客户端确认的只是 **Upstream HTTP 返回 2xx**；必须确认 Upstream 已 `await` 下游服务发布成功且客户端调用了 `EnsureSuccessStatusCode()`。

### 2.2 第二步：数量级闭合检验（证伪优先）

发现任何异常信号（如重连、偶发超时）时，必须先算**理论影响上限**，与实际缺口比对量级：

- **均匀随机 P2P 的断连损失估算公式**：
  $$\text{预期丢失消息数} \approx \left(\sum \text{单连接不可用秒数}\right) \times \frac{\text{每秒总发送消息数}}{\text{总连接数}}$$
  *示例*：1000 连接、每秒共发 300 条（每连接 `0.3` 条/秒）。若日志共有 95 次快速重连、累计离线仅 `11` 连接·秒，则最多解释 $11 \times 0.3 \approx 3.3$ 条丢失——若实际缺口为 `31,730` 条，即可立即证伪“短暂重连导致丢消息”，继续追查未重连的半开僵尸连接。
- **组播 / 广播的存活率核对**：
  若某轮服务端真实存活连接占比为 $p$（`TotalConnectionCount / RoundConnected`），而客户端死连接未重连，则实收率会直接收敛到 $p$ 附近。

### 2.3 第三步：多周期同构基线对比（防掩盖与防异构）

用 `perf-list_test_runs` 拉取同一测试**跨多个发布周期**的无并发污染记录，而非只看相邻一绿一黄两个点：
1. **先验同构性（Apples-to-Apples）**：对比前必须核实两侧的**压测端 Region**（`Location` 变化会改变基础 RTT）、**配置参数**（检查 `Config.Comments` 与轮次/连接上限是否曾被临时下调）、**服务端部署拓扑与实例数**（如独立实例 vs 共享池）以及**容器实际运行的镜像标签**一致；严禁凭代码提交日期猜测线上部署版本。
2. **向前回溯拐点（防多因素抵消掩盖）**：某次历史改动吃光容量余量后，常被同期另一项正向优化或压测端迁区（RTT 降低）刚好抵消，使系统长期悬停在 `98%~99%` 饱和边缘仍显示绿灯，直到后续微小波动才越界。必须看底层绝对资源水位何时发生阶跃。

### 2.4 第四步：单调计数器积分与“未饱和 / 饱和”双区拆解

对多容器架构做跨版本性能归因（区分主进程退化 vs Sidecar 膨胀 vs 宿主瓶颈）时，严格执行两项定量规范：
1. **用累计计数器首尾积分替代分钟桶瞬时值**：分钟级速率指标（`Max` / `Average`）受采样时刻与轮次启停相位差影响会产生奇偶分钟伪抖动。量化单位工作开销（如 `µs/msg`）时，必须在固定总消息数窗口 $[t_0, t_1]$ 上对**单调递增累计计数器**（如 `cpu_usage_core_nanoseconds`）取差值积分：
   $$\text{单位消息 CPU 开销} = \frac{\text{Counter}(t_1) - \text{Counter}(t_0)}{\text{窗口内总消息数}}$$
2. **拆分“未饱和区”与“饱和区”定责**：一旦高负载轮次触及宿主机物理核或容器配额上限，同节点各容器进入零和抢占——Sidecar 多吃 CPU 会压低主容器配额，造成主容器总核秒数“看似下降”的假象。必须**仅在低负载未饱和轮次（节点与各容器均远低于上限）度量各组件自身的纯效率变化**，再在饱和轮次观察整机天花板挤压。

---

## 3. 已知坑点（Pitfalls & Anti-Patterns）

### 3.1 低延迟不能排除丢消息
- **误区（Trap）**：看到报表中 `0-50ms` 占比 `99.8%`，就认为网络和服务完全健康，怀疑是压测统计写错了。
- **真相（Reality）**：[`ClientAgentContext.Measure`](../../src/Pods/Client/ClientAgentContext.cs) 仅在客户端**实际收到消息回调时**才记录延迟样本；没收到的消息根本没有延迟记录。
- **铁律（Rule）**：送达率（`Receive / Expected`）与已达消息延迟分布必须正交独立判读；“65% 连接存活秒收 + 35% 僵尸连接全丢”会同时呈现出 35% 丢包与 99.8% `<50ms` 低延迟。

### 3.2 发现代码隐患不等于找到本次事故主因
- **误区（Trap）**：读代码发现某处没 `await` 或某处没加锁，本地也能构造故障注入复现，就直接宣布找到了线上缺口根因。
- **真相（Reality）**：本地复现只证明“若下游报错会被掩盖”，不证明“线上那次运行真的发生了下游报错”。
- **铁律（Rule）**：必须用当次运行的线上日志/服务指标验证该分支实际触发次数，数量级对得上才能定为主因；否则只能作为顺手加固的次要缺陷。

### 3.3 手动 `start_test` 不按 `Dir` 串行（自制造 429）
- **误区（Trap）**：看到两个测试配在同一个 `Dir`（如 `prod-wcus-dedicated`），以为连续调用两次 `perf-start_test` 会自动排队执行。
- **真相（Reality）**：`start_test` 每次单任务提交 `DirectoryTestCount = 1`，彼此独立并发运行；共享同一目标实例（如 `perfv2wcus`）时会把连接数叠翻倍直接打出 `429`。注意：不同测试即使在同一 `Dir` 也可能指向不同实例（如 `perfv2wcus` vs `perfv2wcuss2`），反过来不同 `Dir` 也可能共用实例（如 `prod-wcus-dedicated` 与 `prod-wcus-dedicated-warmup`）。
- **铁律（Rule）**：手动重跑前必看配置中的实际 `ConnectionString` Endpoint；凡共用同一服务实例的测试必须等前一个 `Cleaned` 后再启动下一个，或统一使用 `start_test_batch`。

### 3.4 黄色 `Suspicious` 可中途恢复，但最终黄灯在流水线验收中不是 Pass
- **误区（Trap）**：看到 `Status = Test Finishes`、`Healthy = true` 就口头汇报“测试通过了”。
- **真相（Reality）**：运行中途单轮不达标会置 `Check = "Suspicious"`，若后续足够多轮次达标（`suspiciousCount` 归零）会清空回绿色 `""`（见 [`TestRunner.cs`](../../src/Pods/Coordinator/TestRunner.cs)）；但只要结束时仍为 `Suspicious`，[`TestStatusController.DirCheck`](../../src/Pods/Portal/Controllers/TestStatusController.cs) 会判定整批目录失败（`fail`）。
- **铁律（Rule）**：区分“执行未崩溃（Healthy）”与“业务达标（Check 为空）”；汇报结果时必须明确列出最终黄色项及其未达标指标。

### 3.5 Azure Table Merge 清空字段陷阱
- **误区（Trap）**：想清除 `TestStatusEntity.Check` 告警时将其赋值为 `null`。
- **真相（Reality）**：[`TableAccessor.UpdateAsync`](../../src/Libs/Common/Tables/TableAccessor.cs) 默认使用 `TableUpdateMode.Merge`，`null` 属性会被服务端忽略，导致旧的 `"Suspicious"` 永久残留。
- **铁律（Rule）**：清空表实体字符串状态必须显式赋 `string.Empty`，读取侧同时将 `null` 与 `""` 视为无告警。

### 3.6 开环压测“尾延迟暴涨 10 倍 = 单次处理慢了 10 倍”错觉（容量悬崖效应）
- **误区（Trap）**：看到高轮次延迟从 `<100ms` 恶化到 `>1s`，就先入为主去代码里找“慢了几倍”的重型退化。
- **真相（Reality）**：开环定速发压（每秒固定注入 $\lambda$、持续 $T$ 秒）在容量边界呈阶跃悬崖：当服务稳态吞吐能力 $\mu > \lambda$ 时排队接近 0；一旦 $\mu$ 因边际开销上升跌破 $\lambda$ 仅 **2%~3%**（如 $\mu = 0.98\lambda$），单轮 $T = 60\text{s}$ 内就会线性积压 $(1 - \mu/\lambda) \times T \approx 1.2\text{s}$ 的队尾延迟。
- **铁律（Rule）**：遇到零丢包、零断连但高轮次延迟分布整体单调右移过 `1s` 时，优先按**“系统有效吞吐天花板小幅下移（2%~10%）”**排查各容器与节点的边际算力增量，而非只找巨幅慢调用。

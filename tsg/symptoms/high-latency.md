# 现象排障：消息延迟超标（High Latency）

## 1. 现象特征（Observable Signature）

- **Portal Report 状态**：🟡 `Thresholds not met`（`Check = "Suspicious"`），触发项为：
  `Messages with latency >= 1s`（要求 `<= 5% of received`）。
- **代码口径说明**：[`ClientAgentContext.cs`](../../src/Pods/Client/ClientAgentContext.cs) 与 [`RoundStatus.cs`](../../src/Libs/Common/Tables/Entities/RoundStatus.cs) 中，桶名 `LessThan2s` 实际代表 **`[1s, 2s)`**，因此慢消息统计式为 `(LessThan2s + LessThan5s + MoreThan5s) / MessageRecieved > 5%`。客户端时间偏差通过 `CoordinatorTime` 校准。

---

## 2. 快速分流表（Triage Matrix）

| 区分信号 | 优先怀疑方向 | 取证动作 |
|---|---|---|
| 随轮次加压（Active 连接 / 吞吐阶梯上升）延迟从 `<100ms` 平滑右移到 `1-2s` / `2-5s`，与同配置历史基线拐点一致 | 压测负载触及目标 SKU 容量水位的预期排队表现 | 对比同配置历史完成记录与服务 `ServerLoad`：[`service-metrics-and-sdks.md#服务指标`](../debugging/service-metrics-and-sdks.md#21-azure-monitor-核心指标对账表) |
| 相同配置、相同轮次下，服务端 `ServerLoad` 未升甚至更低，但延迟远高于历史基线（如翻数倍或高轮次排队过 `1s`） | 服务端主路径退化，或同 Pod 的代理/日志 Sidecar 膨胀打满宿主物理核（公开 `ServerLoad` 盲区） | 多周期同构基线与未饱和/饱和双区拆解：[`methodology-and-pitfalls.md`](../debugging/methodology-and-pitfalls.md#23-第三步多周期同构基线对比防掩盖与防异构)，并下钻容器/节点四维指标：[`service-metrics-and-sdks.md`](../debugging/service-metrics-and-sdks.md#23-服务端内部容器与节点下钻geneva-mdm-四维核对) |
| Client Pod 或 AppServer / Upstream Pod 的 CPU 逼近 Limit、或出现 GC/线程池饥饿 | 压测端自身排队污染了端到端 `Ticks` 计时 | 查 `Perf` 表容器与节点 CPU/内存水位：[`aks-logs-and-nodes.md#查重启与资源`](../debugging/aks-logs-and-nodes.md#22-三张核心表标准查法) |
| 前几轮正常，仅单轮出现少量 `>1s`/`>5s` 尖峰且伴随特定 Pod 重连 | 局部网络抖动或重连恢复期间积压消息释放 | 查当轮重连分布与节点日志：[`aks-logs-and-nodes.md#单节点定位`](../debugging/aks-logs-and-nodes.md#23-单节点网络异常交叉定位法) |

---

## 3. 已验证机制目录（Verified Mechanisms）

### 3.1 阶梯探顶压测触及服务容量上限
- **故障域**：`Service`（容量边界信号）
- **作用机制**：如 `warmup` 等阶梯加压用例设计为持续推高并发吞吐直至接近或超过单实例极限；当总消息吞吐逼近服务上限时，连接虽不断开、消息也不丢失，但队列深度增加导致排队延迟跨过 1 秒线。
- **确诊判据**：前序低负载轮次全部 `<100ms`，固定从某一固定负载轮次开始超标，且历次同配置健康运行拐点高度一致，服务侧 `ServerLoad` 同步升至高位。
- **排除判据**：在远低于历史拐点的轻负载轮次即出现大量 `>=1s` 消息时，不能归结为正常探顶。
- **典型锚点**：`prod-wcus-dedicated-echo-ws-warmup`（10 轮加压至 100k active）长期固定从第 5 轮起进入 `>=1s > 5%` 区间。

### 3.2 同负载下服务端延迟退化（Service Regression / 环境波动）
- **故障域**：`Service`
- **作用机制**：新服务版本发布或后端集群状态变化后，在低于容量上限的常规负载下，服务端连接处理或跨节点路由耗时上升，将原本落在 `200-500ms` / `500-1000ms` 的尾延迟推过 `1s` 阈值。
- **确诊判据**：Client/AppServer Pod CPU 与内存充裕、无重连无丢包，与最近通过的同配置运行相比，相同轮次的延迟分桶整体显著右移。
- **排除判据**：若重跑一次在无干扰窗口立即恢复 `<200ms`，需核查前次是否与大流量测试共享实例或受预热后残余负载影响。
- **典型锚点**：`prod-wcus-dedicated-broadcast-ws-v10` 与 `echo-ws-s2` 在 100% 连满零丢包下偶发高轮次跨过 5% 慢消息阈值，属重点盯防的服务侧性能信号。

### 3.3 压测端（Client / Server Pod）CPU 或时钟偏差导致测量失真
- **故障域**：`Harness / SDK` | `Infra / AKS`
- **作用机制**：端到端延迟由发送端写入 `CoordinatedUtcNow()`、接收端回调时相减得出；若发送/接收容器 CPU 饱和导致收包回调排队，或跨 Pod `TimeBias` 同步异常，会直接放大统计延迟。
- **确诊判据**：`Perf` 表显示 Client 或 AppServer 容器 `cpuUsageNanoCores` 顶满配额，但服务侧自身 `RestApiResponseTimeCount` / 内部延迟正常。
- **排除判据**：容器与节点 CPU 利用率远低于上限（如 `<30%`）时，可排除压测端算力瓶颈。

### 3.4 共享节点下 Sidecar 资源膨胀挤压容量悬崖（局部 `ServerLoad` 盲区）
- **故障域**：`Service`
- **作用机制**：反向代理或日志采集等 Sidecar 容器单消息 CPU 开销上升 → 在物理核数有限的节点上与主运行时争抢 CPU 并打满宿主机物理 vCPU → 集群有效吞吐上限仅下移数个百分点即跌破开环发压速率，单轮 60s 内线性累积出 `>1s` 排队延迟，而仅统计主容器 cgroup 的 `ServerLoad` 因被抢核反而显示偏低。
- **确诊判据**：低负载未饱和轮次中 `proxy / runtime`（或日志容器）累计核秒数显著上涨而主容器未劣化；高负载轮次 `kubernetes_node` 总 CPU 顶满物理核上限（如 8 核节点容器合计 `~7.5` 核 + 内核软中断），延迟呈典型开环队列爬升且零丢包。
- **排除判据**：宿主机 `kubernetes_node` 与各 Sidecar 容器 CPU 均远离节点物理核数与各自 `LimitsCPU` 时，可排除节点算力挤压。
- **典型锚点**：Dedicated 少节点规格（如 3×8 核节点承接 50k msg/s TLS WebSocket）在代理切换/升级与日志 Sidecar 增量叠加后，虽 `ServerLoad` 仅 `~22%` 且主进程更快，整机仍触顶触发尾延迟超标。

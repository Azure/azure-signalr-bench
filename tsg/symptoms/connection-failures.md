# 现象排障：建连阶段失败（Connection Target Not Reached）

## 1. 现象特征（Observable Signature）

- **Portal Report 状态**：🔴 `Failed`，报错阶段为 `round N: connecting`，命令为 `StartClientConnections`。
- **判定规则来源**：[`ClientAgentContainer.cs`](../../src/Pods/Client/ClientAgentContainer.cs) 要求每轮必须连满配置的连接总数；每条新连接最多尝试 **3 次 × 15 秒**（失败后调用 `StopAsync` 重置并间隔 1 秒重试），重试耗尽仍有任意连接失败则整轮报错退出：
  `{failed} of {total} connections failed to connect after 3 attempts. First error: ...`

---

## 2. 快速分流表（Triage Matrix）

| 区分信号 | 优先怀疑方向 | 取证动作 |
|---|---|---|
| 首个错误为 `429 (Too Many Requests)`，且同一时间有多个测试打向同一服务实例 | 同服务并发测试叠加连接/速率超限 | 查并发运行与服务总连接数：[`methodology-and-pitfalls.md#同服务并发陷阱`](../debugging/methodology-and-pitfalls.md#33-手动-start_test-不按-dir-串行自制造-429) |
| 无并发测试，但大比例连接在 3 次重试后仍超时或报 `429` | 目标服务/副本 `ServerLoad` 或连接配额触顶 | 查服务 `ServerLoad` 与握手状态码：[`service-metrics-and-sdks.md#服务指标`](../debugging/service-metrics-and-sdks.md#21-azure-monitor-核心指标对账表) |
| 仅某 1~2 个 Client Pod 报建连超时/握手前断开，其余十几个 Pod 全绿 | 特定 AKS 节点网络卡顿/丢包 | 按 Pod 反查宿主机节点交叉验证：[`aks-logs-and-nodes.md#单节点定位`](../debugging/aks-logs-and-nodes.md#23-单节点网络异常交叉定位法) |
| 第 1 轮刚开始即报连接被拒/握手断开，或紧跟前一个大连接测试之后立即报超限 | AppServer 未真正就绪，或前序测试旧 Pod 残留连接未释放 | 查前序时间窗连接残留与 Ready 时序：[`aks-logs-and-nodes.md#生命周期坑`](../debugging/aks-logs-and-nodes.md#32-shell-形式-entrypoint-吞掉-sigterm残留连接污染下一轮) |
| 重试日志中出现 SDK 内部重连与外层重试交叉报错（集中在 Socket.IO / MQTT / AspNet） | 客户端 SDK 超时或后台自动重连与外层建连重试竞争 | 核对各协议 SDK 建连行为：[`service-metrics-and-sdks.md#sdk-对比`](../debugging/service-metrics-and-sdks.md#22-各协议客户端-sdk-超时与半开行为对照) |

---

## 3. 已验证机制目录（Verified Mechanisms）

### 3.1 同服务多测试并发导致连接/协商超限（429）
- **故障域**：`Scheduling / Operator`
- **作用机制**：单独调用 `start_test` 不会按 `Dir` 排队；两个共用同一专用实例的测试同时启动时，总连接数与建连速率翻倍，触发服务端 `HTTP 429`。
- **确诊判据**：运行记录时间窗重叠，配置中的目标 `Endpoint` 相同，服务侧 `TotalConnectionCount` 接近两场测试之和。
- **排除判据**：时间窗前后无其他运行且服务侧前序连接已归零时，可立即排除。
- **典型锚点**：共用 `perfv2wcus` 的 `smallgroup-ws-v10` 与 `echo-ws-v10` 同时触发时双双在第 3~4 轮报 429；改为串行单跑后均一次全绿。

### 3.2 目标服务或副本真实容量/负载打满
- **故障域**：`Service`
- **作用机制**：单场测试自身的连接规模或建连速率达到目标 SKU / Replica 上限，服务端 `ServerLoad` 逼近 100% 或触发限流，3 次 × 15 秒重试仍无法连满目标值。
- **确诊判据**：无外部并发污染、Client/Server Pod 资源充裕，但服务端 `ServerLoad` 持续高位或 `ClientRequestStatus` 出现批量非 `101` 状态码。
- **排除判据**：若服务端 `ServerLoad` 很低且仅个别客户端节点失败，则不是服务容量瓶颈。
- **典型锚点**：`sio-group-replicabench` 在副本负载打满时约 6% 连接握手超时，按“必须连满目标量”原则应判定失败或确认容量上限。

### 3.3 AKS 单节点网络卡顿导致局部 Pod 建连失败
- **故障域**：`Infra / AKS`
- **作用机制**：集群扩容出的个别节点宿主网络异常（新建连接或 TLS/WebSocket 握手卡顿数十秒），落在其上的 Client Pod 或 AppServer Pod 无法在重试窗口内完成握手。
- **确诊判据**：失败集中于同一 `Computer`（节点名）上的 Pod，同一轮次其他节点上的同构 Pod 建连耗时正常（毫秒级）。
- **排除判据**：若所有节点上的 Client Pod 均匀出现同等比例失败，排除单节点故障。
- **典型锚点**：`20261109-1` 中落在异常节点 `vmss000045` 上的 `warmup` AppServer 建连卡顿 30s、`webpubsub-replicabench-smallgroup` 客户端建连失败。

### 3.4 服务就绪早报（Premature Ready）或前序连接残留
- **故障域**：`Harness / SDK`
- **作用机制**：Server 在 Web 端口监听或后端服务通道建立完成前即上报 `Ready`，导致第 1 轮首波客户端撞上未就绪端点；或前序测试删 Pod 时未收到 `SIGTERM`，旧连接在 30 秒强杀宽限期内仍占用服务配额。
- **确诊判据**：失败集中在 Round 1 最前几秒，或服务侧 `TotalConnectionCount` 在本测试开始瞬间仍残留上一场测试的大量连接。
- **排除判据**：Round 1~2 均顺利连满，到中后轮次才失败，则与启动就绪及前序残留无关。
- **典型锚点**：AppServer / WpsPyServer 就绪时序已在提交 `c8fea917` 修复（等 Kestrel 监听及 SignalR 探针通过后再发 `Ready`）；`AspNetAppServer` 及容器 `ENTRYPOINT` 优雅退出仍需留意。

### 3.5 瞬时单连接网络抖动（仅无重试旧版或超长卡死）
- **故障域**：`Infra / AKS` | `Harness / SDK`
- **作用机制**：数万并发握手下偶发 1~2 条 TLS 重置或包丢失；若单次超时设置与 SDK 默认值冲突（如外层 15s 超时但 SDK 内部 20s 仍在后台重连），会引发状态竞争。
- **确诊判据**：数万连接中仅个位数失败且重试即可恢复，或 SDK 日志出现同一实例并发 `ConnectAsync`。
- **排除判据**：3 次独立重试（每次先 `StopAsync`）后仍大批量失败时，绝不是偶发单包抖动。
- **典型锚点**：提交 `22a049c7` 已引入 3 次 × 15s 有界重试并将 Socket.IO `ConnectionTimeout` 对齐为 15s。

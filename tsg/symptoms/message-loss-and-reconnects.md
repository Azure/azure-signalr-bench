# 现象排障：收消息不足与断连重连（Message Loss & Reconnects）

## 1. 现象特征（Observable Signature）

- **Portal Report 状态**：🟡 `Thresholds not met`（后端 `Check = "Suspicious"`），常见触发项（见 [`RoundStatus.cs`](../../src/Libs/Common/Tables/Entities/RoundStatus.cs)）：
  - `Messages received`：`< 95% of expected`（或实收为 `0`）
  - `Connected connections`：`< 75% of target`
  - `Reconnecting connections`：`> 5%`
- **注意**：即使本页现象同时伴随 **99%+ 已收消息延迟 `< 50ms`**，也完全正常——丢失的消息根本不会进入延迟分桶（详见 [`methodology-and-pitfalls.md#低延迟误区`](../debugging/methodology-and-pitfalls.md#31-低延迟不能排除丢消息)）。

---

## 2. 快速分流表（Triage Matrix）

| 区分信号 | 优先怀疑方向 | 取证动作 |
|---|---|---|
| 客户端报 `Connected = 100%`（重连极少），但服务侧 `TotalConnectionCount` 远低于客户端，且出现大量 `PingTimeout` 关闭 | AKS 节点 TCP 半开黑洞 + SDK 无读超时导致“僵尸半开连接” | 核对服务连接守恒 + 反查客户端所在节点：[`service-metrics-and-sdks.md#连接守恒`](../debugging/service-metrics-and-sdks.md#21-azure-monitor-核心指标对账表)、[`aks-logs-and-nodes.md#单节点定位`](../debugging/aks-logs-and-nodes.md#23-单节点网络异常交叉定位法) |
| 服务侧 `ServerLoad` 接近 100%，或 `ConnectionCloseCount` 出现 `Throttled` / `SlowClient`，客户端同步观测到在线连接跌落或重连 | 服务/副本达到吞吐或慢客户端阈值，发生真实断连或丢弃 | 查服务 `ServerLoad` 与 `ConnectionCloseCategory`：[`service-metrics-and-sdks.md#服务指标`](../debugging/service-metrics-and-sdks.md#21-azure-monitor-核心指标对账表) |
| 发生过断连重连，恢复到 `Connected = 100%` 后**后续所有轮次**组播实收率永久缺一块，但 P2P/Echo 正常 | 客户端重连后未成功重新加组（Group 订阅丢失） | 查客户端重连与 `JoinGroup` 日志及 SDK 版本：[`service-metrics-and-sdks.md#sdk-对比`](../debugging/service-metrics-and-sdks.md#22-各协议客户端-sdk-超时与半开行为对照) |
| 经 Upstream / AppServer HTTP 转发的场景（如 WPS/MQTT P2P、Broadcast）缺消息，且客户端/Upstream 日志有发送警告或 4xx/5xx | 发送路径失败，需先核对 `Sent / Expected` 确认边界 | 核对发送计数确认链：[`methodology-and-pitfalls.md#确认边界`](../debugging/methodology-and-pitfalls.md#21-第一步画清计数确认边界) |
| 仅在轮次切换后出现一批异常断开（WebSocket aborted） | `StopScenario` 取消令牌中断了在途底层 WebSocket 发送 | 查轮次结束时间点的客户端异常日志 |

---

## 3. 已验证机制目录（Verified Mechanisms）

### 3.1 单节点 TCP 半开黑洞 + 无读超时 SDK 形成“僵尸连接”
- **故障域**：`Infra / AKS` + `Harness / SDK`
- **作用机制**：异常 AKS 节点静默丢弃部分已建立 TCP 双向报文（连服务端发回的 FIN/RST 也丢弃）。服务端因收不到心跳以 `PingTimeout` 清理连接；但 `.NET WebPubSubClient` 底层 `ReceiveAsync` 无读超时检测，在纯接收场景（如 P2P / Listen）下永久阻塞在半开 socket 上不重连。客户端以为 100% 在线，而发往已死 `userId` 的 `SendToUser` 在 Web PubSub 中合法返回 `202 Accepted`（无在线连接即静默空投），导致实收率精确跌落到服务端真实存活连接占比。
- **确诊判据**：
  1. 服务侧 `ConnectionCloseCount (PingTimeout)` 持续非零，且 `TotalConnectionCount / RoundConnected` 与当轮消息实收率高度吻合（误差 ~1%）；
  2. 服务侧累计 `Open - 中途 Close` 远小于客户端自报的 `ConnectedCount`，差距远超客户端日志中的 `disconnected` 次数；
  3. 同一时间窗落在该 AKS 节点上的其他测试（如 SignalR `warmup` 单 Pod）重连数独占全集群绝大部分，而落在正常节点上的同配置测试 `PingTimeout = 0`、100% 收满。
- **排除判据**：若服务侧 `PingTimeout == 0` 且 `TotalConnectionCount == RoundConnected`，立即排除本机制。
- **典型锚点**：`20261112-1` 中落在 `vmss00004r` 的 `wps-p2p`（实收 63.7%，服务端存活 ~65%）与 `wps-smallgroup`（实收 74.8%）；同批次落在 `vmss00004w` 的 `wps-biggroup` 330 万条消息 100.00% 零丢失。

### 3.2 服务/副本高负载下真实掉线或慢客户端清理
- **故障域**：`Service`
- **作用机制**：目标服务或跨区副本在高扇出/高负载下出现排队或限流，部分客户端连接断开并在轮次中重连，断连窗口或出站缓冲溢出造成消息未送达。
- **确诊判据**：`ConnectedCount` 在轮次报告中明显低于 `RoundConnected`（或 `TotalReconnectCount` / `ReconnectingCount` 上升），同时服务侧 `ServerLoad` 高企或 `ConnectionCloseCategory` 出现 `SlowClient` / `Throttled`。
- **排除判据**：若仅发生极少量快速重连，先用 [`不可用连接·秒 × 单连接消息率`](../debugging/methodology-and-pitfalls.md#22-第二步数量级闭合检验证伪优先) 计算理论最大丢失量；若算出来只有几条而实际差几万条，绝不能归因于那几次重连。
- **典型锚点**：`sio-group-replicabench--47` 在副本高负载下 `ConnectedCount` 降至 1952~1970/2000，实收 81%~92% 触发黄色告警。

### 3.3 重连后组关系丢失或重连统计掩盖
- **故障域**：`Harness / SDK`
- **作用机制**：客户端断连重连获取新 `connectionId` 后，非可靠子协议不会保留原服务端组订阅；若 SDK 或 Agent 未在重连完成时可靠执行 `JoinGroupAsync`，该连接此后虽在线却再也收不到组播消息。此外，若断开回调误标为 `Closed` 而非 `Reconnecting`，会导致报表 `Reconnected` 假性显示为 `0`。
- **确诊判据**：组播场景在某轮发生过断连后，后续轮次即使无新断连也持续固定比例少收消息；或容器日志有 `is disconnected` 而报表 `Reconnected == 0`。
- **排除判据**：P2P（按 `userId` 路由，不依赖组）出现同等比例缺口时，重连丢组不是主因。
- **典型锚点**：已将 `Azure.Messaging.WebPubSub.Client` 从存在 `AutoRejoinGroups` 缺陷的 `1.0.0-beta.1` 升级至正式版 `1.0.0`，并修复了 [`WebSocketClientAgent.cs`](../../src/Pods/Client/ClientAgent/WebSocketClientAgent.cs) 的 `OnReconnecting` 状态流转。

### 3.4 发送确认边界早于真实发布（假 `Sent / Expected`）
- **故障域**：`Harness / SDK`
- **作用机制**：客户端在 Upstream HTTP 返回后即累加 `MessageSent` 与 `ExpectedRecievedMessageCount`；若 Upstream 先回 `204` 再异步后台调服务发布，或客户端未调用 `EnsureSuccessStatusCode()`，则发布失败也会计入分母，表现为“收小于发”。
- **确诊判据**：Upstream 或客户端日志存在发布 `429/5xx` 异常，或服务侧 `RestApiResponseTimeCount` 请求总数明显少于客户端 `MessageSent`。
- **排除判据**：服务侧收到的发布请求数与 `MessageSent` 一致且无错误响应时，排除此项。
- **典型锚点**：[`CloudEventMiddleware.cs`](../../src/Pods/WpsUpstream/CloudEventMiddleware.cs) 已改为 `await` 发布完成后再返回 `204`，客户端 `SendToAppServer` 已补齐 `EnsureSuccessStatusCode()`。

### 3.5 `StopScenario` 取消令牌中断活跃 WebSocket
- **故障域**：`Harness / SDK`
- **作用机制**：场景停止时取消 `CancellationToken`，若该 token 直接传给底层 `ClientWebSocket.SendAsync`，取消在途发送会使整个 WebSocket 进入 `Aborted` 状态，导致轮次交界处连接异常断开。
- **确诊判据**：断开日志精确集中在每轮结束（`StopScenario`）时刻，且仅影响本轮负责发包的 Active 连接。
- **排除判据**：纯接收连接（Listen）或轮次中间时段持续发生的断开与此无关。

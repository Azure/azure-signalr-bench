# 取证与避坑：服务侧指标语义与各协议客户端 SDK 行为对照

## 1. 适用问题（Questions Answered）

- 在未开启高开销 Resource Diagnostic Logs 的情况下，如何仅凭 Azure Monitor 免费常驻指标判断连接是否真在线、REST 请求是否到达服务、断开原因是什么？
- 为什么指标请求数和客户端发送数一开始对不上？
- 当外部指标确认慢在服务端内部，但公开 `ServerLoad` 很低（如仅 `20%~30%`）甚至比历史版本更低时，如何下钻区分主进程、代理/日志 Sidecar 与宿主机物理核瓶颈？
- 为什么在同样的节点网络丢包下，SignalR 能自动重连恢复，而 Web PubSub 却出现几百个永久挂死的僵尸连接？

---

## 2. 标准取证动作（Standard Probes）

### 2.1 Azure Monitor 核心指标对账表

对目标 SignalR / Web PubSub 资源查询 1 分钟粒度（`PT1M`）指标时，严格按以下真实语义解读：

| 指标名称 | 推荐聚合与关键维度 | 真实语义与对账用法 |
|---|---|---|
| `TotalConnectionCount` | `Average`, `Maximum` | 服务端视角当前真实存活连接数。**直接与报表 `ConnectedCount` 对比**：若客户端报 `1000` 而此处仅 `~650`，说明存在大量半开僵尸连接或客户端状态漏计。 |
| `ConnectionOpenCount` | `Total` | 新建立成功的连接总数。应等于 `初始目标连接数 + 实际重连成功次数`（与客户端 SDK `is connected` 日志条数严格相等）。 |
| `ConnectionCloseCount` | `Total`，按 `ConnectionCloseCategory` 切分 | 重点看 `Normal`、`PingTimeout`（服务端收不到客户端心跳超时踢线）、`Throttled`、`SlowClient`、`ServiceReload`。**连接守恒公式**：测试期间 `Open - 中途 Close` 必须等于测试结束杀 Pod 时产生的 `Close` 总数。 |
| `ClientRequestStatus` | `Total`，按 `ClientType`, `Status` 切分 | **仅代表客户端 WebSocket/MQTT/Socket.IO 握手状态码**（正常为 `101`），用于排查建连阶段 `429` / `401` / `5xx`。 |
| `RestApiResponseTimeCount` | `Total`，按 `RestApiCategory`, `ResponseTime` 切分 | 统计到达服务端的 REST 调用次数与耗时分桶（如 `SendToUser`、`SendToAll`），可验证 Upstream 请求是否到达服务端及服务端处理延迟。 |
| `ServerLoad` | `Average`, `Maximum` | 服务端主运行时 cgroup CPU 除以逻辑配额的百分比。接近 `80%~100%` 可确认主进程逼近配额；但因其**不含代理/日志 Sidecar 且分母未必等于底层 VM 物理核数**，**低数值绝不能单独用来排除宿主机物理 CPU 饱和**（详见 2.3 与坑点 3.3）。 |

查询示例：
```powershell
az monitor metrics list --resource <RESOURCE_ID> `
  --metrics ConnectionCloseCount `
  --start-time 2026-11-12T05:35:00Z --end-time 2026-11-12T05:53:00Z `
  --interval PT1M --aggregation Total `
  --filter "ConnectionCloseCategory eq '*'" -o json
```

### 2.2 各协议客户端 SDK 超时与半开行为对照

排查断连、重连与半开卡死时，必须区分不同协议 Agent 底层 SDK 的机制差异：

| 协议 / Agent | 心跳与读超时（半开检测） | 断连重连与加组恢复机制 |
|---|---|---|
| **SignalR Default / Serverless**（[`SignalRClientAgentBase`](../../src/Pods/Client/ClientAgent/SignalRClientAgentBase.cs)） | 双向保护：客户端每 `15s` 发应用层 Ping，且有 **`30s` `ServerTimeout` 读超时**。遇到 TCP 半开黑洞会在 30s 内主动掐断 socket 并触发重连。 | 内置自动重连；`Reconnecting` → `Reconnected` 回调中重新写连接 ID 并调用 `OnConnected` 重新加组。 |
| **Web PubSub WebSocket**（[`WebSocketClientAgent`](../../src/Pods/Client/ClientAgent/WebSocketClientAgent.cs)，`Azure.Messaging.WebPubSub.Client 1.0.0`） | 底层 `.NET ClientWebSocket` 默认每 `30s` 发单向 Pong（忽略发送异常），**但 `ReceiveOneFrameAsync` 无任何读超时检测**。若网络黑洞同时丢弃服务端发回的 FIN/RST，纯接收客户端会**永久阻塞在 `ReceiveAsync` 形成半开僵尸连接**。 | `RawWebSocketJson` 不做有状态恢复，断开触发 `Disconnected`（标为 `Reconnecting`）后走 `AutoReconnect` 建新连接，在 `Connected` 中由 `OnConnected` 重新 `JoinGroupAsync` 并累加 `TotalReconnectCount`。 |
| **Socket.IO**（[`SioClientAgent`](../../src/Pods/Client/ClientAgent/SioClientAgent.cs)，`SocketIOClient 3.0.8`） | Engine.IO 心跳机制；`ConnectionTimeout` 已显式设为 **`15s`**（与外层单次建连尝试超时一致，防止默认 20s 造成后台悬挂重连竞争）。 | 默认无限次自动重连，`OnReconnectAttempt` 标 `Reconnecting`，重连成功后重新加组。 |
| **MQTT / Legacy AspNet SignalR**（[`MqttClientAgent`](../../src/Pods/Client/ClientAgent/MqttClientAgent.cs)、[`AspNetSignalRClientAgent`](../../src/Pods/Client/ClientAgent/AspNetSignalRClientAgent.cs)） | MQTT 依赖 KeepAlive 周期；AspNet 2.x 老客户端取消支持弱（活跃发送无法通过 `CancellationToken` 中断）。 | 在 `Disconnected` / `Closed` 事件里自行发起后台重连循环，排查建连重试时需留意后台重连与外层重试并发竞争。 |

### 2.3 服务端内部容器与节点下钻（Geneva MDM 四维核对）

当压测端与外部指标已确认瓶颈在服务端内部，需通过对应 Region 的 Geneva MDM 账号下钻核对以下四个正交维度，定位究竟是哪个容器或哪层资源触顶：

| 核对维度 | 命名空间 / 指标 | 取证目的 |
|---|---|---|
| **1. 实例拓扑与分片数** | `ShoeboxInternal`（按 `ResourceId`, `InstanceId` 切分） | 确认活跃 Pod 数量与部署形态（如独立 Pod vs 多租户共享多进程池）在基线与当前运行间是否发生架构切换。 |
| **2. 容器真实运行镜像** | `kube_state_prometheus` / `kube_pod_container_info`（维度 `pod`, `container`, `image`） | 直接读取当次运行各容器的精确镜像标签，实锤线上实际跑的组件版本。 |
| **3. 容器级累计 CPU 拆分** | `kubernetes_pod_container` / `cpu_usage_core_nanoseconds`（维度 `pod_name`, `container_name`, `node`） | 对主运行时容器、反向代理 Sidecar、日志/监控 Sidecar 分别做窗口首尾差值积分，对比各容器单消息开销与 `proxy / runtime` 比值。 |
| **4. 宿主机物理核水位** | `kubernetes_node` / `cpu_usage_nanocores`（维度 `node`） | 核对节点总用户态 CPU 是否已逼近 VM 物理 vCPU 数（当容器合计达物理核数的 `90%~95%` 时，叠加内核网络/TLS 软中断即已 100% 满载）。 |

---

## 3. 已知坑点（Pitfalls & Anti-Patterns）

### 3.1 把 `ClientRequestStatus` 误当成 REST 发布状态码，或把 `RestApiResponseTimeCount` 误当成投递成功证明
- **误区（Trap）**：看到 `ClientRequestStatus` 全是 `101` 没有 `429`，或者看到 `RestApiResponseTimeCount (SendToUser)` 数量等于 `99,000` 且全在 `<100ms`，就断定“所有消息都已被服务端成功投递给在线用户，没收到就是服务端吞消息”。
- **真相（Reality）**：
  1. `ClientRequestStatus` 只统计客户端连接握手，完全不包含 REST API；
  2. `RestApiResponseTimeCount` 不按 HTTP 响应码分维度；
  3. 更关键的是，Web PubSub 的 `SendToUser` REST API 在**目标 `userId` 当前没有任何在线连接时，本来就会在 `<100ms` 内合法返回 `202 Accepted`**（空投无报错）。
- **铁律（Rule）**：看到 `SendToUser` 请求数对齐但客户端少收时，**第一步永远先去核对 `TotalConnectionCount` 与 `ConnectionCloseCount (PingTimeout)`**，检查目标用户当时在服务端是否真的在线。

### 3.2 指标查询窗口紧贴测试结束秒数截断（漏算尾部）
- **误区（Trap）**：测试在 `05:48:26Z` 结束，查询 Azure Monitor 时把 `--end-time` 设为 `05:49:00Z`，结果发现服务侧 REST 请求总数比客户端 `MessageSent` 少了七八千条，误判为请求在半路丢了。
- **真相（Reality）**：服务端指标按分钟桶聚合且有 1~2 分钟管线刷盘延迟，最后一轮尾部的请求与杀 Pod 时的 `ConnectionCloseCount` 往往落在结束后第 1~2 个分钟桶内。
- **铁律（Rule）**：对账总量时，`--end-time` 必须向后多留 **3~5 分钟** 直至分钟桶归零，并同时检查前后时间窗是否有相邻测试重叠污染。

### 3.3 凭局部容器指标（如 `ServerLoad` 低或下降）排除整机饱和或误判主进程退化
- **误区（Trap）**：看到公开 `ServerLoad` 只有 `20%+`（甚至比旧版更低）就断定服务端算力充裕；反之看到 `ServerLoad` 变高就直接断定主运行时代码劣化。
- **真相（Reality）**：多容器 Pod 共享宿主机物理核时，局部 cgroup 指标存在两类失真：① 若 `ServerLoad` 只采集主容器 cgroup 且分母采用逻辑配额（可能大于实际分配的 VM 物理总核数），当同 Pod 的代理/日志 Sidecar 开销暴涨把宿主机物理核打满时，主容器因抢不到 CPU 时间片，其 `ServerLoad` 读数反而会**不升反降**；② 若目标资源在两次运行间切换了底层部署拓扑（如分片 Pod 数改变导致跨节点转发比例变化），主容器汇总负载也会随之改变。
- **铁律（Rule）**：凡做服务端跨版本性能归因，严禁单看 `ServerLoad`；必须同时对齐 **`主容器 CPU` + `各 Sidecar 容器 CPU` + `宿主机总 CPU` + `部署拓扑/镜像标签`** 四项证据。

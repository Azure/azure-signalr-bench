# Azure SignalR / Web PubSub Perf Bench 排障指南（TSG）

本目录沉淀性能压测（Perf Bench）中验证过的故障模式、取证手法与度量陷阱。  
知识库按 **“现象路由 (`symptoms/`) × 取证与避坑 (`debugging/`)”** 正交组织；维护与扩充前请务必遵守 [`META.md`](./META.md)。

---

## 1. 从现象出发（Step 1：看 `symptoms/`）

打开 Portal 的 **Test Report** 弹窗，根据顶部状态与表格定位入口：

| Report 状态 | 典型信号 | 进入排障入口 |
|---|---|---|
| 🔴 **Failed** | `Connection target not reached`、`StartClientConnections failed`、`429`、握手断开、建连超时 | [`symptoms/connection-failures.md`](./symptoms/connection-failures.md) |
| 🔴 **Failed** | `changed instance/role`、`unsupported Ready protocol`、`Command ... timed out; missing senders`、Ready 2 分钟超时 | [`symptoms/pod-lifecycle-and-control.md`](./symptoms/pod-lifecycle-and-control.md) |
| 🟡 **Thresholds not met** | `Messages received < 95%`、`Received = 0`、`Connected < 75%`、`Reconnecting > 5%` | [`symptoms/message-loss-and-reconnects.md`](./symptoms/message-loss-and-reconnects.md) |
| 🟡 **Thresholds not met** | `Messages with latency >= 1s > 5%`（连接在线且收发数量基本对齐，但慢消息超标） | [`symptoms/high-latency.md`](./symptoms/high-latency.md) |

---

## 2. 取证手法与高频避坑（Step 2：查 `debugging/`）

分流后按需组合以下取证平面，避免掉进已踩过的度量与查询坑：

| 取证平面 | 解决什么问题 | 文档入口 |
|---|---|---|
| **方法论与认知避坑** | 怎么核对 `Sent / Expected / Received` 计数链？怎么定量估算重连影响？手动重跑与状态判断有哪些坑？ | [`debugging/methodology-and-pitfalls.md`](./debugging/methodology-and-pitfalls.md) |
| **AKS 容器日志与节点排查** | Pod 已经被 Cleaned 怎么查日志？怎么识别单节点网络黑洞、OOM 重启或旧 Pod 连接残留？ | [`debugging/aks-logs-and-nodes.md`](./debugging/aks-logs-and-nodes.md) |
| **服务侧指标与 SDK 特性** | Web PubSub / SignalR 各监控指标到底代表什么？时间窗口怎么截才不漏数？不同客户端 SDK 遇到半开连接表现有何差异？ | [`debugging/service-metrics-and-sdks.md`](./debugging/service-metrics-and-sdks.md) |

---

## 3. 标准 3 分钟快查流程

1. **先读 Report 顶部摘要**：确认是硬中断（🔴 Failed）还是单轮/多轮指标未达标（🟡 Thresholds not met），进入对应 `symptoms/` 页面。
2. **先排除自制造干扰与计数错觉**：对照 [`methodology-and-pitfalls.md`](./debugging/methodology-and-pitfalls.md) 检查是否有同服务并发运行、前序残留连接或窗口截断。
3. **跨平面交叉对账**：用 [`aks-logs-and-nodes.md`](./debugging/aks-logs-and-nodes.md) 查客户端/Server Pod 日志与所在节点，用 [`service-metrics-and-sdks.md`](./debugging/service-metrics-and-sdks.md) 查服务侧连接开闭与负载，做到**时间线与数量级双闭合**后再下结论。

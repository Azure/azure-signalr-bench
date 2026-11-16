# 现象排障：Pod 实例异常、Ready 失败与控制命令超时

## 1. 现象特征（Observable Signature）

- **Portal Report 状态**：🔴 `Failed`，常见于 `Ready` 阶段或压测运行中途，典型摘要（见 [`TestFailureSummary.cs`](../../src/Libs/Common/TestFailureSummary.cs) 与 [`TestRunner.cs`](../../src/Pods/Coordinator/TestRunner.cs)）：
  - `A test pod reported a different process identity or role during the run`（原始异常含 `changed instance/role`）
  - `Timed out waiting for test pods to respond to <Command>. Missing pods: ...`
  - `A pod sent an invalid readiness message`（原始异常含 `unsupported Ready protocol`）
  - 启动阶段等待 Server/Client Ready 超时退出。

---

## 2. 快速分流表（Triage Matrix）

| 区分信号 | 优先怀疑方向 | 取证动作 |
|---|---|---|
| 报 `participant <pod> changed instance/role` | 测试运行中途 Pod 崩溃 / OOMKilled 被 K8s 重启，新进程生成了新 `InstanceId` | 查 `KubePodInventory` 重启次数与 `Perf` 内存/CPU 曲线：[`aks-logs-and-nodes.md#查重启与资源`](../debugging/aks-logs-and-nodes.md#22-三张核心表标准查法) |
| 报 `unsupported Ready protocol` 或 `InstanceId is required` | Coordinator 与工作负载镜像版本不匹配 | 核对 KeyVault `Image-*` secret 与发布组件范围 |
| 等待 Server `Ready` 超时，Server Pod 自身报错退出 | Default 模式 SignalR 探针未通过（某 Endpoint 协商或服务连接不通） | 查 Server Pod 启动日志中的失败 Endpoint 与最后异常：[`aks-logs-and-nodes.md#查容器日志`](../debugging/aks-logs-and-nodes.md#22-三张核心表标准查法) |
| 报 `Command <Cmd> timed out; missing senders: ...` | 缺失 Ack 的 Pod 进程阻塞/死锁/GC 停顿、节点失联，或已提前退出 | 查对应 `missing senders` Pod 在超时时间点前后的日志与 CPU/内存：[`aks-logs-and-nodes.md#查容器日志`](../debugging/aks-logs-and-nodes.md#22-三张核心表标准查法) |

---

## 3. 已验证机制目录（Verified Mechanisms）

### 3.1 工作负载内存泄漏 / 同步阻塞导致容器 OOM 重启（`changed instance/role`）
- **故障域**：`Harness / SDK` | `Service`
- **作用机制**：Pod 在高并发请求下发生内存暴涨或未捕获崩溃，被 kubelet 杀掉原地拉起；新进程启动后重新发送带新 `InstanceId` 的 `ReportReady`，被 Coordinator 判定为参与者实例替换并立即中止整场测试。
- **确诊判据**：`KubePodInventory` 中对应 Pod 的 `ContainerRestartCount > 0`，或 `Perf` 表中 `memoryWorkingSetBytes` 在重启前冲顶触及容器内存上限。
- **排除判据**：`ContainerRestartCount == 0` 且容器进程启动时间未变时，排除容器重启。
- **典型锚点**：`signalr-replicabench-smallgroup-sl` 曾因 `SignalRUpstream` 每次 negotiate 新建 `DefaultAzureCredential` 与 `ServiceManager` 并同步拉取 Entra Token 导致 OOM 重启（提交 `3de31f30` 改为单例 `EndpointHubContexts` + 异步预热后根治）。

### 3.2 AppServer 启动就绪探针失败（Endpoint 不可用）
- **故障域**：`Service` | `Infra / AKS`
- **作用机制**：[`AzureSignalRReadinessCheck`](../../src/Pods/AppServer/Readiness/AzureSignalRReadinessCheck.cs) 在上报 `Ready` 前会逐个探测所有配置 Endpoint 的服务端连接（Group Ack）与客户端协商；若某个副本/实例不可达或鉴权失败超过 2 分钟，AppServer 主动报错退出而非带病进场。
- **确诊判据**：AppServer 容器日志包含未通过探针的具体 Endpoint URL 与底层异常，Coordinator 停留在等待 Server Ready。
- **排除判据**：日志已打印 `Server ready acked.` 后才发生的失败，不属于就绪探针失败。
- **典型锚点**：提交 `c8fea917` 引入生命周期 `StartedAsync` + Endpoint 双路就绪探针（注意：`ServiceEndpoint.Online` 初始恒为 `true`，不可作为就绪依据）。

### 3.3 控制面命令 Ack 超时（`missing senders`）
- **故障域**：`Harness / SDK` | `Infra / AKS`
- **作用机制**：Coordinator 通过 Redis 发布控制命令（如 `StartClientConnections`、`StopScenario`）后，部分 Pod 未在超时窗口内回传 `AckMessage`。常见于客户端在 `StopScenario` 等待底层不可取消的发送完成、线程池耗尽，或所在节点失联。
- **确诊判据**：异常明确列出缺失的 `missing senders` 列表；比对这些 Pod 的最后日志时间戳与所在节点状态即可定位卡死位置。
- **排除判据**：若 Pod 已回传 `AckStatus.Faulted`，会直接走远程异常分支（报具体错误原因），不会表现为 `missing senders` 超时。
- **典型锚点**：Redis 控制面早期曾因每次命令临时 `SUBSCRIBE` 造成订阅间隙丢 Ack，已在提交 `0b181aeb` 改为 [`AckHandler`](../../src/Libs/Common/Redis/AckHandler.cs) 每命令单条常驻订阅按 `AckId` 路由。

### 3.4 滚动升级漏发镜像导致 Ready 协议不兼容
- **故障域**：`Scheduling / Operator`
- **作用机制**：Coordinator 要求所有 `ReportReady` 必须携带合法 `Role` 与非空 `InstanceId`，若某类工作负载仍在使用旧镜像则直接触发协议校验失败。
- **确诊判据**：在 `Ready` 阶段秒挂，报错 `unsupported Ready protocol or invalid sender/role`。
- **排除判据**：所有参与组件镜像时间戳均为最新一次同批构建时可排除。

# 取证与避坑：AKS 容器日志、资源水位与异常节点排查

## 1. 适用问题（Questions Answered）

- 测试跑完变成 `Cleaned`、Pod 已被删除，且个人账号没有 Storage Blob 数据面 RBAC 读 `signalrperfv2sa/logs` 时，去哪查完整容器日志？
- 怎么快速查出某个测试的 Client / Server Pod 跑在哪台 AKS 宿主机、有没有发生过 OOM 重启、CPU/内存是否吃满？
- 怎么实锤“某一台 AKS 节点网络抽风/半开黑洞”，而不是服务端问题？

---

## 2. 标准取证动作（Standard Probes）

### 2.1 Container Insights 查询入口

`centralus` 压测集群（`signalrperfv2aks-centralus`）全量采集了容器标准输出与节点指标到 Log Analytics：
- **Workspace CustomerId**：`c6e493ba-26ce-443e-8577-4ac497fa51fc`（资源名 `defaultworkspace-9caf2a1e-9c49-49b6-89a2-56bdec7e3f97-eus`）
- **免交互 CLI 调用方式**（将 KQL 写入临时 JSON 文件 `{"query":"..."}` 后请求）：
  ```powershell
  az rest --method post `
    --url 'https://api.loganalytics.io/v1/workspaces/c6e493ba-26ce-443e-8577-4ac497fa51fc/query' `
    --resource 'https://api.loganalytics.io' `
    --body '@query.json' -o json
  ```

### 2.2 三张核心表标准查法

1. **查容器日志（`ContainerLogV2`）——定位连接错误、重连与异常堆栈**
   ```kusto
   ContainerLogV2
   | where TimeGenerated between (datetime(<START_UTC>) .. datetime(<END_UTC>))
   | where PodName startswith "<TEST_ID>"
   | extend Log = tostring(LogMessage)
   | where Log has_any ("warn", "fail", "error", "exception", "disconnected", "attempt")
   | project TimeGenerated, PodName, Log
   | order by TimeGenerated asc
   ```

2. **查 Pod 落点节点与重启次数（`KubePodInventory`）——排查 `changed instance/role` / OOM**
   ```kusto
   KubePodInventory
   | where TimeGenerated between (datetime(<START_UTC>) .. datetime(<END_UTC>))
   | where Name startswith "<TEST_ID>"
   | summarize FirstSeen=min(TimeGenerated), LastSeen=max(TimeGenerated),
               Nodes=make_set(Computer, 4), MaxRestarts=max(ContainerRestartCount)
     by Name
   ```

3. **查容器与节点 CPU / 内存水位（`Perf`）——排查算力瓶颈或内存冲顶**
   ```kusto
   Perf
   | where TimeGenerated between (datetime(<START_UTC>) .. datetime(<END_UTC>))
   | where ObjectName == "K8SContainer" and InstanceName has "<TEST_ID>"
   | where CounterName in ("cpuUsageNanoCores", "memoryWorkingSetBytes")
   | summarize AvgVal=avg(CounterValue), PeakVal=max(CounterValue) by CounterName, InstanceName
   ```

### 2.3 单节点网络异常交叉定位法

当单客户端 Pod 测试（如 `ClientNum = 1` 的 WPS 用例）出现诡异断连/半开，或多 Pod 测试仅个别分片报错时，用三步交叉锁定坏节点：
1. 在 `KubePodInventory` 查出异常 Pod 所在的 `Computer`（例如 `aks-linux1-...-vmss00004r`）。
2. 查**同一时间段**落在该 `Computer` 上的所有其他压测 Pod（例如 20 分片的 `warmup`），看 Coordinator 日志中该分片 Pod 的 `totalReconnected` 是否独占全集群 95%+ 的重连次数。
3. 对照前后几十分钟落在**其他节点**（如 `aks-linux2`）上的同协议测试是否 `PingTimeout = 0` 且 100% 收满。三项同时成立即可 100% 锁定单节点宿主网络故障。

---

## 3. 已知坑点（Pitfalls & Anti-Patterns）

### 3.1 KQL 保留关键字导致语法报错
- **误区（Trap）**：在 `summarize` / `project` 中使用 `first`、`last`、`kind` 等未转义标识符做输出列名，导致 API 返回 400 语法错误。
- **真相（Reality）**：这些词在 KQL 中是运算符或保留字。
- **铁律（Rule）**：统一使用 `FirstTime`、`LastTime`、`Category` / `EventType` 等非保留列名。

### 3.2 Shell 形式 `ENTRYPOINT` 吞掉 `SIGTERM`（残留连接污染下一轮）
- **误区（Trap）**：以为 Coordinator 调用 `DeleteClientPodsAsync` / `DeleteServerPodsAsync` 后，旧测试的客户端连接会立刻优雅断开。
- **真相（Reality）**：[`Initialize/docker/default.docker`](../../Initialize/docker/default.docker) 使用了 shell 形式 `ENTRYPOINT dotnet ${POD}.dll`，PID 1 为 `/bin/sh` 不会转发 `SIGTERM`，Pod 会等满 30 秒宽限期才被 `SIGKILL` 强杀，期间连接仍在服务端存活。
- **铁律（Rule）**：分析紧邻上一场测试之后的第 1~2 分钟指标时，必须先核对服务侧 `ConnectionCloseCount` 与 `TotalConnectionCount` 是否混入了上一场延迟释放的连接。

### 3.3 .NET 控制台两行式日志在关键字过滤时丢头部或丢正文
- **误区（Trap）**：只按 `where LogMessage has "Azure.Messaging.WebPubSub.Client"` 过滤日志，结果只拿到时间戳/Logger 头，没拿到下一行的错误正文。
- **真相（Reality）**：默认 Console Logger 一条日志分成两行输出（第 1 行 `info: Category[EventId]`，第 2 行缩进正文 `      Connection ...`）。
- **铁律（Rule）**：先按正文关键词定位精确时间戳，再按 `TimeGenerated between (...)` 拉取前后数秒的原始未过滤日志上下文。

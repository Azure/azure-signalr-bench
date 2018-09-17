# CPU & Memory query for SignalR Pod through MDM

This project wants to setup a MDM service for non-windows platform query.

Two projects are created: one is the REST service, which calls an external exe to query MDM. The another is an executable binary which is modified based on MDM sample code(https://microsoft.sharepoint.com/teams/WAG/EngSys/Monitor/SiteAssets/AmdWiki/Home/MDMetricsClientSampleCode.zip).

## Install MDMQuery Service on Windows

Use `build.cmd` to build the solution. "QueryAPI" folder contains the final binary: `MDMSystemLoadQueryService.exe`.

Execute this exe, which will listen on 5353 port.

Please specify where your external executable binary is and where the output can be saved to in appsettings.json. The following is an example setting.


`{
  "MDMExePath": "E:\\home\\Work\\signalr-bench\\MDMSystemLoad\\MDMetricsClientSampleCode\\bin\\Debug\\MDMetricsClientSampleCode.exe",
  "ResultFilePath": "E:\\home\\Work\\signalr-bench\\MDMSystemLoad\\MDMSystemLoadQueryService\\mdm_result.json"
}`

Use nssm(https://nssm.cc/) to setup a windows service.

## REST API to query

### CPU usage query

The following table shows an example to query CPU usage for SignalR pod `signalr-4ae4106f-532e-42bc-a3b2-b40aba221516-557b66f474-8s5dg`

HTTP Method | Request URL | Parameters | Response
------------|-------------|------------|---------
`GET` | `http://<dnsname-or-ip>:5353/mdm/query` | platform=Dogfood systemLoad=CPU podName=signalr-4ae4106f-532e-42bc-a3b2-b40aba221516-557b66f474-8s5dg dateStart="2018-09-16T19:52:29Z" dateEnd="2018-09-16T20:02:28Z" | `{"Definition":{"Id":{"MonitoringAccount":"SignalRShoeboxTest","MetricNamespace":"systemLoad","MetricName":"PodCpuUsage"},"StartTimeUtc":"2018-09-17T03:52:00","EndTimeUtc":"2018-09-17T04:02:00","SamplingTypes":[{"Name":"Max"}],"AggregationType":7,"DimensionCombination":[{"Key":"podName","Value":"signalr-4ae4106f-532e-42bc-a3b2-b40aba221516-557b66f474-8s5dg"}]},"StartTimeUtc":"2018-09-17T03:52:00","EndTimeUtc":"2018-09-17T04:02:00","ErrorCode":0,"Datapoints":[{"TimestampUtc":"2018-09-17T03:52:00","Value":52.0},{"TimestampUtc":"2018-09-17T03:53:00","Value":51.0},{"TimestampUtc":"2018-09-17T03:54:00","Value":50.0},{"TimestampUtc":"2018-09-17T03:55:00","Value":50.0},{"TimestampUtc":"2018-09-17T03:56:00","Value":51.0},{"TimestampUtc":"2018-09-17T03:57:00","Value":52.0},{"TimestampUtc":"2018-09-17T03:58:00","Value":50.0},{"TimestampUtc":"2018-09-17T03:59:00","Value":50.0},{"TimestampUtc":"2018-09-17T04:00:00","Value":50.0},{"TimestampUtc":"2018-09-17T04:01:00","Value":50.0},{"TimestampUtc":"2018-09-17T04:02:00","Value":52.0}]}`

### Memory usage query

The following table shows an example to query Memory usage for SignalR pod `signalr-4ae4106f-532e-42bc-a3b2-b40aba221516-557b66f474-8s5dg`

HTTP Method | Request URL | Parameters | Response
------------|-------------|------------|---------
`GET` | `http://<dnsname-or-ip>:5353/mdm/query` | platform=Dogfood systemLoad=Memory podName=signalr-4ae4106f-532e-42bc-a3b2-b40aba221516-557b66f474-8s5dg dateStart="2018-09-16T19:52:29Z" dateEnd="2018-09-16T20:02:28Z" | `{"Definition":{"Id":{"MonitoringAccount":"SignalRShoeboxTest","MetricNamespace":"systemLoad","MetricName":"PodMemory"},"StartTimeUtc":"2018-09-17T03:52:00","EndTimeUtc":"2018-09-17T04:02:00","SamplingTypes":[{"Name":"Max"}],"AggregationType":7,"DimensionCombination":[{"Key":"podName","Value":"signalr-4ae4106f-532e-42bc-a3b2-b40aba221516-557b66f474-8s5dg"}]},"StartTimeUtc":"2018-09-17T03:52:00","EndTimeUtc":"2018-09-17T04:02:00","ErrorCode":0,"Datapoints":[{"TimestampUtc":"2018-09-17T03:52:00","Value":355332096.0},{"TimestampUtc":"2018-09-17T03:53:00","Value":355250176.0},{"TimestampUtc":"2018-09-17T03:54:00","Value":355278848.0},{"TimestampUtc":"2018-09-17T03:55:00","Value":355270656.0},{"TimestampUtc":"2018-09-17T03:56:00","Value":355504128.0},{"TimestampUtc":"2018-09-17T03:57:00","Value":356179968.0},{"TimestampUtc":"2018-09-17T03:58:00","Value":356122624.0},{"TimestampUtc":"2018-09-17T03:59:00","Value":356478976.0},{"TimestampUtc":"2018-09-17T04:00:00","Value":356134912.0},{"TimestampUtc":"2018-09-17T04:01:00","Value":356143104.0},{"TimestampUtc":"2018-09-17T04:02:00","Value":356323328.0}]}`

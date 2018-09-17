# CPU & Memory query for SignalR Pod through MDM

## Install MDMQuery Service on Windows

Use `build.cmd` to build the solution. "QueryAPI" folder contains the final binary: `MDMSystemLoadQueryService.exe`.

Execute this exe, which will listen on 5353 port.

## REST API to query

### CPU usage query

HTTP Method | Request URL | Parameters | Response
------------|-------------|------------|---------
`GET` | `http://<dnsname-or-ip>:5353/mdm/query` | platform=Dogfood systemLoad=CPU podName=XXX dateStart="2018-09-16T19:52:29Z" dateEnd="2018-09-16T20:02:28Z" | `{"Definition":{"Id":{"MonitoringAccount":"SignalRShoeboxTest","MetricNamespace":"systemLoad","MetricName":"PodMemory"},"StartTimeUtc":"2018-09-17T03:52:00","EndTimeUtc":"2018-09-17T04:02:00","SamplingTypes":[{"Name":"Max"}],"AggregationType":7,"DimensionCombination":[{"Key":"podName","Value":"signalr-4ae4106f-532e-42bc-a3b2-b40aba221516-557b66f474-8s5dg"}]},"StartTimeUtc":"2018-09-17T03:52:00","EndTimeUtc":"2018-09-17T04:02:00","ErrorCode":0,"Datapoints":[{"TimestampUtc":"2018-09-17T03:52:00","Value":355332096.0},{"TimestampUtc":"2018-09-17T03:53:00","Value":355250176.0},{"TimestampUtc":"2018-09-17T03:54:00","Value":355278848.0},{"TimestampUtc":"2018-09-17T03:55:00","Value":355270656.0},{"TimestampUtc":"2018-09-17T03:56:00","Value":355504128.0},{"TimestampUtc":"2018-09-17T03:57:00","Value":356179968.0},{"TimestampUtc":"2018-09-17T03:58:00","Value":356122624.0},{"TimestampUtc":"2018-09-17T03:59:00","Value":356478976.0},{"TimestampUtc":"2018-09-17T04:00:00","Value":356134912.0},{"TimestampUtc":"2018-09-17T04:01:00","Value":356143104.0},{"TimestampUtc":"2018-09-17T04:02:00","Value":356323328.0}]}`

### Memory usage query

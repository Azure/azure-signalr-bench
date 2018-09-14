Different scenario may contains one or more configs. All configs are in yaml format.

# Example of Echo Scenario
`
serviceType: selfhost
transportType: Websockets
hubProtocol: json
scenario: echo
connection: 100000
concurrentConnection: 1000
duration: 60
interval: 1
messageSize: 2k
serverUrl: wanleastusagentsAppSvrDNS0.eastus.cloudapp.azure.com:5050/signalrbench
pipeline:
- createConn
- startConn
- up5000
- scenario
- up5000
- scenario
- stopConn
- disposeConn
`
# Rpc Master Scripts Guide

## Echo/Broadcast/P2P Scenario

### Script Sample
```
cd ~/workspace/signalr_auto_test_framework/signalr_bench/Rpc/Bench.Client/
dotnet run -- --rpcPort 5555 --duration 10 --connections 5 --interval 1 --slaves 2 \
--serverUrl 'http://localhost:5050/signalrbench' \
--pipeLine 'createConn;startConn;up1;scenario;up1;scenario;up1;scenario;up1;scenario;up1;scenario;stopConn;disposeConn' \
-v unit1 -t Websockets -p json -s sendToClient  \
--slaveList 'localhost;localhost'  \
--debug 'debug' \
--retry 0  --clear false -o 'counters.txt'
```

### Explanation

The main update is supporting the the number of connections which sending messages can be less than the connections. And the number of connections of sending messages can be increased dynamically.

#### Scenario
* Echo: ` -s echo `
* Broadcast: ` -s broadcast `
* P2P: ` -s sendToClient `

Pipeline should be `--pipeLine 'createConn;startConn;up1;scenario;up1;scenario;up1;scenario;up1;scenario;up1;scenario;stopConn;disposeConn'`

`up1` names add one more connection to send message in next epoch.
Only `up1`, `up2`, `up5`, `up100`, `up200`, `up500`, `up1000`, `up2000`, `up5000` can be used.

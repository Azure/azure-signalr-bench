# Rpc Master Scripts Guide

## Echo/Broadcast/P2P Scenario

### Script Sample
```
cd <YOUR_RPC_MASTER_FOLDER>
dotnet run -- --rpcPort 5555 --duration 10 --connections 5 --interval 1 \
--serverUrl 'http://localhost:5050/signalrbench' \
--pipeLine 'createConn;startConn;up1;scenario;up1;scenario;up1;scenario;up1;scenario;up1;scenario;stopConn;disposeConn' \
-v unit1 -t Websockets -p json -s sendToClient  \
--slaveList 'localhost;localhost'  \
--debug 'debug' \
--messageSize '2k' \
--retry 0  --clear false -o 'counters.txt'
```

### Explanation

The main update is supporting the the number of connections which sending messages can be less than the connections. And the number of connections of sending messages can be increased dynamically.

#### Scenario
* Echo: ` -s echo `
* Broadcast: ` -s broadcast `
* P2P: ` -s sendToClient `
* Group: `-s SendGroup`

Pipeline should be `--pipeLine 'createConn;startConn;up1;scenario;up1;scenario;up1;scenario;up1;scenario;up1;scenario;stopConn;disposeConn'`

`up1` names add one more connection to send message in next epoch.
Only `up1`, `up2`, `up5`, `up100`, `up200`, `up500`, `up1000`, `up2000`, `up5000` can be used.


#### Debug mode
Set `--debug 'debug'` to debug in localhost, which will try to localhost:5555 and localhost:6666.
In this mode, the slaveList should be `--slaveList 'localhost;localhost'`, and slave number should be `--slaves 2`


### Usage
dotnet run -- [options]

### Options
```
--messageSize: Determine the message size sent between client and server. Default: "0". Example: "256", "2K", "2k".

```

### Examples
#### Frequent join/leave group
```
dotnet run -- --rpcPort 5555 --duration 10 --connections 100 --interval 1 \
--serverUrl 'http://localhost:5050/signalrbench' \
--pipeLine 'createConn;startConn;upPerGroup2;clearJoinLeaveCounters;configMessageCountPerInterval1;freqJoinLeaveGroup;stopConn;disposeConn' \
-v unit1 -t Websockets -p json -s 'SendGroup'  \
--slaveList 'localhost;localhost'  \
--debug 'debug' \
--messageSize '2' \
--groupNum 4 \
--groupOverlap 1 \
--concurrentConnection 100 \
--retry 0  --clear false -o 'counters.txt'
```

`upPerGroup2` means 2 connection in a group will send messages.
`clearJoinLeaveCounters` means clear counters for the freqJoinLeaveGroup scenario.
`configMessageCountPerInterval1` means 1 message will be sent in the interval specified.
`freqJoinLeaveGroup` means the scenario of frequently join and leave groups.

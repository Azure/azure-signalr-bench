## Local Latency tool for Azure SignalR
This is a small tool to test echo latency between the Client/Server and the Azure SignalR Service.

The latency is defined as a round trip of a message: Client <=> Azure SignalR Service <=> AppServer

This tool hosts the client and server in the same process. You need to offer a connection string as a program param to run the bench.

## Download and run the tool
```bash
git clone https://github.com/Azure/azure-signalr-bench.git -b localBench --single-branch

cd azure-signalr-bench/src/Pods/LocalBench
```

### On a linux Amdx64 machine
```bash
chmod +x LocalBench
./LocalBench -c "connectionString"
```
### On other platforms
Modify the build.sh to build the tool for your platform.


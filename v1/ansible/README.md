# Target
The ansible playbooks are used to setup necessary environment for SignalR Service Perf test.

* setup_signalr_bench.sh
It is used to setup all softwares and environment configuration. Please specify your VM hostname or IP in signalr_hosts.
In addtion, you need to specify your ssh login user in some of the playbooks.

* update_websocket.sh
websocket-bench may update at any time. Please update all of binaries using this script.

* update_azuresignalrchatsample.sh
Update the Azure SignalR Service ChatSample.

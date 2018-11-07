# Target

This bench is composed of scripts to build SignalR package, run SignalR benchmarks, collect result, and visualize it. It targets to compare SignalR and SignalR service's performance for many scenarios.

# Usage

* Install & Configure Nginx

Nginx server is used to host the html pages for visualizing result. Install Nginx, create a folder $HOME/NginxRoot, and make that folder be the root directory.


* Start bench

Specify the server list of SignalR and SignalR service. See bench_service_* and bench_app_*. Specify the client servers. Its format is "hostname1:port1:login_user1|hostname2:port2:login_user2|..."

Launch the SignalR and SignalR service apps (currently this step is manually)

The benchmark scenarios are: echo and broadcast, with JSON and MessagePack protocols. For SignalR selfhost and SignalR service mode, we use `selfhost` and `service` to distinguish them. In config_env.sh, you can set `bench_name_list`, `bench_type_list`, and `bench_codec_list` per your requirement. Each of them contains at least one item.

sh run.sh

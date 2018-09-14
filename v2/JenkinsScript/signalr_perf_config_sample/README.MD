## Folder Structure

### /config
Config for running scripts. More details in config/README.MD

### /scenarios
Scenarios for benchmark. More delete in scenarios/README.MD

### /scripts
Script to create/delete VMs, run benchmarks. More details in scripts/README.MD



## Auto Test Pipeline

### Setup Environment
Setup configurations in config/ folder.

> Notice that the number of AzureSignalrConnectionString should be the same as the number of app servers. String should be seperated by "^".

Then active environment:

`source config/environments.sh`

### Create Bench Server (Create Only Once)
Create bench server VM. Make sure this VM has large disks to store informations, logs, statistics and results for benchmark.

`sh scripts/createBenchVm.sh`

Then update `BenchServerGroup`, `VnetName` and `SubnetName` in config/environments.sh

Then active environment:

`source config/environment.sh`

### Create App Server and agent VMs
Create app server VMs in the same VNet of bench server VM.

`sh scripts/createVms.sh`

### Setup Benchmark Configurations
Setup benchmark configurations in scenarios/. There are some configuration samples in sub folders.

### Update server url in benchmark configurations
Set environment variable `JobConfig` to the benchmark configuration you want to update.

`export JobConfig='PATH_TO_JOB_CONFIG'`

`sh scripts/updateServerUrl.sh`

### Run Benchmark

Set environment variable `JobConfig` to the benchmark configuration you want to run.
`export JobConfig='PATH_TO_JOB_CONFIG'`

`sh scripts/runBenchMark.sh`

All data will be stored in `~/signalr-statistics-<suffix>` folder, all logs will be stored in `logs` subfolder, all cpu usage will be stored in `machine` subfolder and counters' records in `results` subfolder.

### Delete App Server And Client VMs.

`sh scripts/deleteResourceGroup.sh`

All script sample can be found in scripts/README.MD




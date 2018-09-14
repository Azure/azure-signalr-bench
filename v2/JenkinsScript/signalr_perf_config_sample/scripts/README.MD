# Create bench server
`sh createBenchVm.sh`

# Create App server and client VMs in the with specific Vnet
`sh createVms.sh`

# Update server URL after creating/updating app server VM/ client VMs
`sh updateServerUrl.sh`

# Run scenario benchmark
Update job config file
`export JobConfig='PATH_TO_JOB_CONFIG'`
Run benchmark
`sh runBenchMark.sh`

# Delete App server VM and client VM
`sh deleteResourceGroup.sh`

To run any of the scripts above, you should setup configuration in config/ folder first.

export AzureStorageConnectionString='xxx'
export ConfigBlobContainerName='xxx'
export ServicePrincipalFileName='xxx.yaml'

export ConfigRoot='xxx/'
export RootFolder='xxx/'
export ScenarioRoot='xxx/'

export JobConfig=$ScenarioRoot'echo/job.yaml'
export AgentConfig=$ConfigRoot'agent.yaml'
export BenchConfig=$ConfigRoot'bench.yaml'

export PrivateIps=$RootFolder'privateIps.yaml'
export PublicIps=$RootFolder'publicIps.yaml'

export ResultFolderSuffix='suffix'
export SendToFixedClient='true'
# number of AzureSignalrConnectionString should be the same as the number of app servers. String should be seperated by ^
export AzureSignalrConnectionString='xxx'
export Branch='remotes/origin/xxx'

export BenchServerGroup='xxx'
export VnetName='xxx'
export SubnetName='xxx'
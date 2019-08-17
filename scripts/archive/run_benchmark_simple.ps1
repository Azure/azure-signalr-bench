Stop-Job *
Remove-Job *

$current_dir = Get-Location
$project_root =  "$current_dir\.."
$benchmark = $Args[0]
$azure_signalr_connection_string = $Args[1]

$generate_proto = {
    & cd $project_root\SignalRServiceBenchmarkPlugin\framework\rpc\
    & ./generate_protos.ps1
    cd $current_dir
}

$start_appserver = { 
    $env:Azure__SignalR__ConnectionString = $using:azure_signalr_connection_string
    & cd ($using:project_root + "\SignalRServiceBenchmarkPlugin\utils\AppServer")
    dotnet clean
    dotnet build
    dotnet run -- --urls=http://*:5050 > appserver.log
    & cd $using:current_dir
}

$start_agents = {
    Write-Host 'Start agent'
    cd ($using:project_root + "\SignalRServiceBenchmarkPlugin\framework\agent\")
    dotnet clean
    dotnet build
    dotnet run -- --HostName 0.0.0.0 --RpcPort 5555
    cd $using:current_dir
}

$generate_report = {
    Write-Host 'Generate report'
    cd $current_dir
    report_simple_dist_windows\report_simple\report_simple.exe $args[1]
    Write-Host 'Report saved as ./report.svg'
}

$start_master = {
    sleep 10
    echo 'Start master'
    $project_master = ($using:project_root + "\SignalRServiceBenchmarkPlugin\framework\master\")
    cd ($project_master)
    dotnet clean
    dotnet build
    cd ($using:current_dir)
    dotnet run -p $project_master -- --BenchmarkConfiguration ($using:benchmark) --AgentList localhost:5555
    cd $using:current_dir
}

Invoke-Command $generate_proto

Start-Job $start_appserver -Name "appserver"
Start-Job $start_agents -Name "agents"
Start-Job $start_master -Name "master"

$master_finish = 0
while($master_finish -eq 0)
{
    foreach($job in Get-Job)
    {
        Receive-Job $job
        if ($job.Name -eq "master")
        {
            $state = [string]$job.State
            if($state -eq "Completed")
            {   
                Write-Host($job.Name + " finish")
                $master_finish = 1
            }
        }
    }
    sleep 1
}

Stop-Job *
Remove-Job *
Invoke-Command $generate_report -ArgumentList $current_dir,"counters.txt"


"All tasts finish"



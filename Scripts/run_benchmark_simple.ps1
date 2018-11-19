Stop-Job *
Remove-Job *

$current_dir = Get-Location
$project_root = $Args[0]
$benchmark = $Args[1]
$azure_signalr_connection_string = $Args[2]

Write-Host $current_dir
Write-Host $project_root
Write-Host $azure_signalr_connection_string

$start_appserver = { 
    $env:Azure__SignalR__ConnectionString = $args[1]
    cd ($args[0] + "\SignalRServiceBenchmarkPlugin\utils\AppServer")
    dotnet build
    dotnet run -- --urls=http://*:5050 > appserver1.log
}

$start_slaves = {
    echo 'Start slave'
    cd ($args[0] + "\SignalRServiceBenchmarkPlugin\framework\slave\")
    dotnet build
    dotnet run -- --HostName 0.0.0.0 --RpcPort 5555
}

$generate_report = {
    Write-Host 'Generate report'
    cd $args[0]
    report_simple_dist_windows\report_simple\report_simple.exe $args[1]
    Write-Host 'Report saved as ./report.svg'
}

$start_master = {
    sleep 10
    echo 'Start master'
    cd ($args[0] + "\SignalRServiceBenchmarkPlugin\framework\master\")
    dotnet build
    dotnet run -- --BenchmarkConfiguration $args[1] --SlaveList localhost:5555
}

Start-Job $start_appserver -Name "appserver" -ArgumentList $project_root,$azure_signalr_connection_string
Start-Job $start_slaves -Name "slaves" -ArgumentList $project_root
Start-Job $start_master -Name "master" -ArgumentList $project_root,$benchmark

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
Invoke-Command $generate_report -ArgumentList $current_dir,($project_root+ "\SignalRServiceBenchmarkPlugin\framework\master\counters_oneline.txt")


"All tasts finish"



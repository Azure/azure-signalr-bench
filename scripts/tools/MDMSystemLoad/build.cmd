dotnet build
xcopy MDMetricsClientSampleCode/bin/Debug MDMetrics /O /X /E /H /K
cd MDMSystemLoadQueryService
publish.cmd
cd ..

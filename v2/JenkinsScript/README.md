# Manage SignalR Service in Dogfood environment
commands examples:

Register Dogfood cloud
`dotnet run --ExtensionScriptsDir "/home/hongjiang/hz_signalr_auto_test_framework/signalr_bench/JenkinsScript/ExternalScripts" -S "RegisterDogfoodCloud" --utils /home/hongjiang/secrets/utils.sh`

Azure lgoin and create SignalR service
`dotnet run --ExtensionScriptsDir "/home/hongjiang/hz_signalr_auto_test_framework/signalr_bench/JenkinsScript/ExternalScripts" -S "CreateDogfoodSignalr" --ResourceGroupLocation "southeastasia" --utils /home/hongjiang/secrets/utils.sh`

Delete SignalR service
`dotnet run --ExtensionScriptsDir "/home/hongjiang/hz_signalr_auto_test_framework/signalr_bench/JenkinsScript/ExternalScripts" -S "DeleteDogfoodSignalr" --utils /home/hongjiang/secrets/utils.sh --ResourceGroup group26825820 --SignalRService sr26825820`

Unregister Dogfood cloud
`dotnet run --ExtensionScriptsDir "/home/hongjiang/hz_signalr_auto_test_framework/signalr_bench/JenkinsScript/ExternalScripts" -S "UnregisterDogfoodCloud" --utils /home/hongjiang/secrets/utils.sh`


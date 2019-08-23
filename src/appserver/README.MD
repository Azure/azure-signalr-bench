# App Server Scripts Guide

## Script Sample
```
cd <YOUR_APPSERVER_FOLDER>
export Azure__SignalR__ConnectionString='<YOUR_AZURE_SIGNALR_CONNECTION_STRING>'
export useLocalSignalR=true
dotnet run
```

## Explanation
If `export useLocalSignalR=true`, the app server will use a local dotnet core SignalR instead of Azure SignalR Service.
Otherwise, you should provide your Azure SignalR connection string.
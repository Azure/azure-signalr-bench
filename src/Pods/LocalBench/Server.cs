namespace LocalBench;

public static class Server
{
    public const string Url = "http://localhost:8100";
    public static async Task RunAsync(string connectionString, CancellationToken token)
    {
        var host = Host.CreateDefaultBuilder()
            .ConfigureLogging((context, logging) =>
            {
                logging.ClearProviders();
                logging.AddConsole(options =>
                {
                    options.TimestampFormat = "hh:mm:ss yyyy/MM/dd";
                });
            })
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string>
                    {
                        {"Azure:SignalR:ConnectionString", connectionString}
                    });
                });
                webBuilder.UseStartup<Startup>().UseUrls(Url);
            }).Build();
        
        Console.WriteLine("Start server...");
        await host.RunAsync(token);
    }
}
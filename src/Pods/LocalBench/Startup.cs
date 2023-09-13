namespace LocalBench;

public class Startup
{
    private const string HubName = "/signalrbench";


    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        var builder =services.AddSignalR();
        var connectionString = Configuration["Azure:SignalR:ConnectionString"];
        builder.AddAzureSignalR(connectionString);
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseRouting();
        app.UseAzureSignalR(routes => { routes.MapHub<BenchHub>(HubName); });
    }
}
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using SignalRStreaming;

namespace SignalRChat
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var hubBuffer = new HubBuffer();
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .ConfigureServices(s => s.AddSingleton(hubBuffer))
                .Build().Run();
        }
    }
}

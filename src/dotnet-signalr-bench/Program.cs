using McMaster.Extensions.CommandLineUtils;
using System.Threading.Tasks;

namespace Microsoft.Azure.SignalR.PerfTest.AppServer
{
    class Program
    {
        static async Task Main(string[] args)
        {
            await CommandLineApplication.ExecuteAsync<CommandLineOptions>(args);
        }
    }
}

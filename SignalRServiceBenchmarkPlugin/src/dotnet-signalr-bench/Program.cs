using McMaster.Extensions.CommandLineUtils;

namespace Microsoft.Azure.SignalR.PerfTest.AppServer
{
    class Program
    {
        static int Main(string[] args)
        {
            return CommandLineApplication.Execute<CommandLineOptions>(args);
        }
    }
}

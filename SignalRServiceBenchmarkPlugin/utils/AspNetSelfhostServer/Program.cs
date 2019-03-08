using System;
using Microsoft.Owin.Hosting;

namespace AspNetSelfhostServer
{
    class Program
    {
        static void Main(string[] args)
        {
            var config = new Configuration();
            var options = new StartOptions();
            options.Urls.Add(config.Url);
            using (WebApp.Start<Startup>(options))
            {
                Console.WriteLine($"Server running at {config.Url}");
                Console.ReadLine();
            }
        }
    }
}

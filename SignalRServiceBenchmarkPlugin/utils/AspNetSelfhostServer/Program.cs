using System;
using Microsoft.Owin.Hosting;

namespace AspNetSelfhostServer
{
    class Program
    {
        static void Main(string[] args)
        {
            var config = new Configuration();
            using (WebApp.Start<Startup>(config.Url))
            {
                Console.WriteLine($"Server running at {config.Url}");
                Console.ReadLine();
            }
        }
    }
}

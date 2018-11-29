using System;
using Microsoft.Owin.Hosting;

namespace AspNetSelfhostServer
{
    class Program
    {
        static void Main(string[] args)
        {
            using (WebApp.Start<Startup>("http://localhost:8009"))
            {
                Console.WriteLine("Server running at http://localhost:8009/");
                Console.ReadLine();
            }
        }
    }
}

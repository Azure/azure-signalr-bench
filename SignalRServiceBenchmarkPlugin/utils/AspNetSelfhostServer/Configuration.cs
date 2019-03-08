using System;
using System.Configuration;
using System.Net;
using System.Net.Sockets;

namespace AspNetSelfhostServer
{
    internal class Configuration
    {
        private readonly string _connectionString;
        private readonly string _url;

        public string ConnectionString => _connectionString;

        public string Url => _url;

        public bool UseLocalSignalR = false;

        public int ConnectionCount = 15;

        public Configuration()
        {
            var settings = ConfigurationManager.ConnectionStrings;
            _connectionString = settings != null ?
                                settings["Azure:SignalR:ConnectionString"].ConnectionString :
                                Environment.GetEnvironmentVariable("Azure:SignalR:ConnectionString");
            var localSignalR = ConfigurationManager.AppSettings["UseLocalSignalR"];
            var signalRType = localSignalR != null ? localSignalR : Environment.GetEnvironmentVariable("UseLocalSignalR");
            if (!String.IsNullOrEmpty(signalRType) && Boolean.TryParse(signalRType, out bool useLocalSignalR))
            {
                UseLocalSignalR = useLocalSignalR;
            }
            var url = Environment.GetEnvironmentVariable("WebServerUrl");
            if (String.IsNullOrEmpty(url))
            {
                var ip = GetIP();
                if (!String.IsNullOrEmpty(ip))
                {
                    url = $"http://{ip}:5050";
                }
            }
            _url = String.IsNullOrEmpty(url) ? "http://localhost:5050" : url;
        }

        private string GetIP()
        {
            string ip = null;
            string hostName = Dns.GetHostName(); // Retrive the Name of HOST
            // Get the IP
            foreach (var addr in Dns.GetHostEntry(hostName).AddressList)
            {
                if (addr.AddressFamily == AddressFamily.InterNetwork)
                {
                    ip = addr.ToString();
                }
            }
            return ip;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AspNetSelfhostServer
{
    internal class Configuration
    {
        private readonly string _connectionString;
        private readonly string _url;

        public string ConnectionString => _connectionString;

        public string Url => _url;

        public Configuration()
        {
            _connectionString = Environment.GetEnvironmentVariable("ASRSConnectionString");
            _url = Environment.GetEnvironmentVariable("WebServerUrl");
        }
    }
}

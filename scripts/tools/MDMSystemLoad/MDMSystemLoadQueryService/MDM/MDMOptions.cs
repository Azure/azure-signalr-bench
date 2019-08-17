using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MDMSystemLoadQueryService
{
    public class MDMOptions
    {
        public static readonly string CertificateThumbprintDefaultKey = "Azure:SignalR:MDM:CertificateThumbprint";

        public string CertificateThumbprint { get; set; } = null;

        public string EndpointUrl { get; set; } = null;

        public string ExternalExePath { get; set; } = null;

        public string ResultPath { get; set; } = null;
    }
}

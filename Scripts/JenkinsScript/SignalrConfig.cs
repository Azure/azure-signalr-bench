using System;
using System.Collections.Generic;
using System.Text;

namespace JenkinsScript
{
    class SignalrConfig
    {
        public string AppId { get; set; }
        public string Password { get; set; }
        public string Tenant { get; set; }
        public string Location { get; set; }
        public string BaseName { get; set; }
        public string Sku { get; set; }

        public string Subscription { get; set; }
    }
}

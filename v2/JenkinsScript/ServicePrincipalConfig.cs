using System;
using System.Collections.Generic;
using System.Text;

namespace JenkinsScript
{
    public class ServicePrincipalConfig
    {
        public string ClientId { get; set; }
        public string TenantId { get; set; }
        public string ClientSecret { get; set; }
        public string Subscription { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace azuremonitor
{
    class ServicePrincipal
    {
        public string ClientId { get; set; }
        public string TenantId { get; set; }
        public string ClientSecret { get; set; }
        public string Subscription { get; set; }
    }
}

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace MDMSystemLoadQueryService
{
    public class MDMOptionsSetup : IConfigureOptions<MDMOptions>
    {
        private readonly string _certificateThumbprint;
        private readonly string _defaultEndpoint = "https://shoebox2.metrics.nsatc.net/public/monitoringAccount/SignalRShoeboxTest/homeStamp";

        public MDMOptionsSetup(IConfiguration configuration)
        {
            _certificateThumbprint = configuration.GetSection(MDMOptions.CertificateThumbprintDefaultKey).Value;
        }

        public void Configure(MDMOptions options)
        {
            if (options.CertificateThumbprint == null)
            {
                options.CertificateThumbprint = _certificateThumbprint;
            }

            if (options.EndpointUrl == null)
            {
                options.EndpointUrl = _defaultEndpoint;
            }
        }
    }
}

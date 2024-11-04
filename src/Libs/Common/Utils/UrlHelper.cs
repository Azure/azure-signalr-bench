using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace Azure.SignalRBench.Common
{
    public class UrlHelper
    {
        public static string ParseBaseUrlForWebPubSub(Uri uri)
        {
            var build = new UriBuilder(uri);
            build.Query = null;
            var baseUrl = build.Uri.AbsoluteUri;

            var query = QueryHelpers.ParseQuery(uri.Query);
            if (query.TryGetValue("hub", out var hub))
            {
                baseUrl = QueryHelpers.AddQueryString(baseUrl, "hub", hub);
            }

            return baseUrl;
        }
    }
}

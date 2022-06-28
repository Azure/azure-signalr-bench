using System;
using System.Collections.Generic;
using System.Text;
using Azure.SignalRBench.Common;
using Xunit;

namespace CommonTest
{
    public class ReliableWebsocketClientTests
    {
        [Fact]
        public void ParseUrlTest()
        {
            var uri = new Uri("http://host/clients/hubs/ahub?access_token=x&vvv=xxx");
            Assert.Equal("http://host/clients/hubs/ahub", UrlHelper.ParseBaseUrlForWebPubSub(uri));

            uri = new Uri("http://host/clients?hub=ahub&access_token=x&vvv=xxx");
            Assert.Equal("http://host/clients?hub=ahub", UrlHelper.ParseBaseUrlForWebPubSub(uri));
        }
    }
}

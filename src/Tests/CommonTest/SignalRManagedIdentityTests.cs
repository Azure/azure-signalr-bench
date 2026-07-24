using System;
using Azure.SignalRBench.Common;
using Xunit;

namespace CommonTest
{
    public class SignalRManagedIdentityTests
    {
        [Fact]
        public void ParseEndpointsAcceptsUrisAndStripsConnectionStringSecrets()
        {
            var endpoints = SignalRManagedIdentity.ParseEndpoints(
                "Endpoint=https://first.service.signalr.net;AccessKey=secret;Version=1.0,https://second.service.signalr.net");

            Assert.Equal(new Uri("https://first.service.signalr.net"), endpoints[0]);
            Assert.Equal(new Uri("https://second.service.signalr.net"), endpoints[1]);
        }

        [Fact]
        public void ParseEndpointsRejectsMissingConfiguration()
        {
            Assert.Throws<ArgumentException>(() => SignalRManagedIdentity.ParseEndpoints(null));
        }

        [Theory]
        [InlineData(TestCategory.AspnetCoreSignalR, true)]
        [InlineData(TestCategory.AspnetCoreSignalRServerless, true)]
        [InlineData(TestCategory.AspnetSignalR, true)]
        [InlineData(TestCategory.RawWebsocket, false)]
        [InlineData(TestCategory.WebPubSubPythonSdk, false)]
        [InlineData(TestCategory.SocketIO, false)]
        [InlineData(TestCategory.Mqtt, false)]
        public void UsesSignalRManagedIdentityOnlyForSignalRCategories(TestCategory category, bool expected)
        {
            Assert.Equal(expected, category.UsesSignalRManagedIdentity());
        }
    }
}
using System;
using System.Net;
using System.Net.Http;
using System.Security.Authentication;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.SignalRBench.Common;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Formatter;
using MQTTnet.Protocol;
using Newtonsoft.Json;

namespace Azure.SignalRBench.Client.ClientAgent
{
    sealed class MqttClientAgent : IClientAgent
    {
        public ClientAgentContext Context { get; }
        public int GlobalIndex { get; }
        private string[] Groups { get; }
        private IMqttClient Connection { get; }
        private readonly string _appserverUrl;
        private readonly ILogger<MqttClientAgent> _logger;
        private readonly MqttClientOptions _options;
        private readonly int _publishQos;
        private readonly int _subscribeQos;

        private static readonly HttpClient HttpClient = new HttpClient();
        private static readonly MqttFactory Factory = new MqttFactory();

        public MqttClientAgent(string url, string appserverUrl, string[] groups,
            int globalIndex, int publishQos, int subscribeQos,
            ClientAgentContext context,
            ILoggerFactory loggerFactory)
        {
            Context = context;
            _appserverUrl = "http://" + appserverUrl;
            Groups = groups;
            GlobalIndex = globalIndex;
            _publishQos = publishQos;
            _subscribeQos = subscribeQos;
            _logger = loggerFactory.CreateLogger<MqttClientAgent>();

            Connection = Factory.CreateMqttClient();
            _options = new MqttClientOptionsBuilder().WithProtocolVersion(MqttProtocolVersion.V500)
                .WithWebSocketServer(o => o.WithUri(url))
                .WithTlsOptions(o =>
                {
                    o.UseTls(true);
                    o.WithSslProtocols(SslProtocols.Tls13);

                })
                .WithCleanSession(false)
                .WithSessionExpiryInterval(30)
                .WithClientId(globalIndex.ToString())
                .Build();

            Connection.ApplicationMessageReceivedAsync += e =>
            {
                var data = JsonConvert.DeserializeObject<RawWebsocketData>(
                    e.ApplicationMessage.ConvertPayloadToString());
                Context.Measure(data.Ticks, data.Payload);
                return Task.CompletedTask;
            };
            Connection.DisconnectedAsync += async e =>
            {
                if (!e.ClientWasConnected)
                {
                    // If it has never succeeded, don't reconnect
                    return;
                }
                await Context.OnReconnecting(this);
                await Task.Delay(1000);
                await Connection.ConnectAsync(_options, CancellationToken.None);
            };
            Connection.ConnectedAsync += async e => { await Context.OnConnected(this, Groups.Length > 0); };
        }

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {

            await Connection.ConnectAsync(_options, cancellationToken);
        }

        public async Task JoinGroupAsync()
        {
            var mqttSubscribeOptions = Factory.CreateSubscribeOptionsBuilder()
                .WithTopicFilter(
                    f =>
                    {
                        f.WithQualityOfServiceLevel((MqttQualityOfServiceLevel)_subscribeQos);
                        f.WithTopic(Groups[0]);
                    })
                .Build();

            await Connection.SubscribeAsync(mqttSubscribeOptions, CancellationToken.None);
        }


        public async Task StopAsync()
        {
            await Connection.DisconnectAsync();
        }

        //This method should be sent directly to appserver to lower pressure on wps runtime 
        public Task BroadcastAsync(string payload)
        {
            var data = new RawWebsocketData()
            {
                Type = "broadcast",
                Ticks = ClientAgentContext.CoordinatedUtcNow(),
                Payload = payload
            };
            return SendToAppServer(data);
        }

        public Task EchoAsync(string payload)
        {
            throw new NotImplementedException();
        }

        public Task GroupBroadcastAsync(string group, string payload)
        {
            // Reuse the RawWebsocketData as a wrapper for the payload, adding timestamp
            var data = new RawWebsocketData()
            {
                Ticks = ClientAgentContext.CoordinatedUtcNow(),
                Payload = payload
            };
            var applicationMessage = new MqttApplicationMessageBuilder()
                .WithTopic(group)
                .WithQualityOfServiceLevel((MqttQualityOfServiceLevel)_publishQos)
                .WithPayload(data.Serilize())
                .Build();
            return Connection.PublishAsync(applicationMessage, CancellationToken.None);
        }

        //This method is sent directly to appserver to lower pressure on wps runtime 
        public Task SendToClientAsync(int index, string payload)
        {
            var data = new RawWebsocketData()
            {
                Type = "p2p",
                Ticks = ClientAgentContext.CoordinatedUtcNow(),
                Payload = payload,
                Target = $"user{index}"
            };
            return SendToAppServer(data);
        }


        private async Task SendToAppServer(RawWebsocketData data)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, _appserverUrl)
            {
                Version = HttpVersion.Version20,
            };
            var binaryData = BinaryData.FromObjectAsJson(data);
            request.Content = new StringContent(binaryData.ToString(), Encoding.UTF8, "application/json");
            await HttpClient.SendAsync(request);
        }
    }

}
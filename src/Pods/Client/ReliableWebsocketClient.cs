using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.SignalRBench.Common;
using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using static Azure.SignalRBench.Client.ClientAgent.WebSocketClientAgent;

namespace Azure.SignalRBench.Client
{
    public class ReliableWebsocketClient
    {
        public const string SubProtocol = "json.reliable.webpubsub.azure.v1";

        private const string ReconnectionTokenKey = "awps_reconnection_token";
        private const string WebPubSubConnectionIdKey = "awps_connection_id";

        private readonly SequenceId _sequenceId = new SequenceId();
        private readonly Uri _originalUri;
        private string _baseUrl;

        private volatile string? _connectionId;
        private volatile string? _reconnectionToken;
        private ClientWebSocket? _socket;

        public State ConnectionState = State.NotStart;

        public ReliableWebsocketClient(Uri uri)
        {
            _originalUri = uri;
            _baseUrl = UrlHelper.ParseBaseUrlForWebPubSub(uri);
        }

        public Action<long, string>? OnMessage;

        public Action? OnClose;

        public Task ConnectAsync(CancellationToken token)
        {
            if (ConnectionState != State.NotStart)
            {
                throw new InvalidOperationException($"Current state {ConnectionState.ToString()} is not ready for connect");
            }

            _ = Task.Run(async () =>
            {
                while (ConnectionState != State.Closed)
                {
                    try
                    {
                        if (_sequenceId.TryGetSequenceId(out var sequenceId))
                        {
                            _ = SendAsync(new SequenceAck(sequenceId).Serialize());
                        }
                    }
                    finally
                    {
                        await Task.Delay(1000);
                    }
                }
            });

            return ConnectAsyncCore(_originalUri, token);
        }

        public Task CloseAsync(WebSocketCloseStatus status, string description, CancellationToken token)
        {
            ConnectionState = State.Closed;
            return _socket?.CloseAsync(status, description, token) ?? Task.CompletedTask;
        }

        public Task SendAsync(string payload)
        {
            return _socket?.SendAsync(Encoding.UTF8.GetBytes(payload), WebSocketMessageType.Text, true, default) ?? Task.CompletedTask;
        }

        public Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
        {
            return _socket?.SendAsync(buffer, messageType, endOfMessage, cancellationToken) ?? Task.CompletedTask;
        }

        private async Task ConnectAsyncCore(Uri uri, CancellationToken token)
        {
            _socket = NewClientWebSocket();
            await _socket.ConnectAsync(uri, token);
            ConnectionState = State.Connected;

            _ = Task.Run(() => ReceiveLoop());
        }

        private ClientWebSocket NewClientWebSocket()
        {
            var socket = new ClientWebSocket();
            socket.Options.AddSubProtocol(SubProtocol);
            return socket;
        }

        private async Task ReceiveLoop()
        {
            var disableReconnection = false;
            try
            {
                while (_socket.State == WebSocketState.Open)
                {
                    var ms = new MemoryStream();
                    Memory<byte> buffer = new byte[1 << 10];
                    // receive loop
                    while (true)
                    {
                        var receiveResult = await _socket.ReceiveAsync(buffer, default);
                        // Need to check again for NetCoreApp2.2 because a close can happen between a 0-byte read and the actual read
                        if (receiveResult.MessageType == WebSocketMessageType.Close)
                        {
                            if (_socket.CloseStatus == WebSocketCloseStatus.PolicyViolation)
                            {
                                disableReconnection = true;
                            }

                            try
                            {
                                Console.WriteLine(
                                    $"The connection closed, status code:{_socket.CloseStatus},description: {_socket.CloseStatusDescription}");

                                await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, default);
                            }
                            catch (Exception e)
                            {
                                // It is possible that the remote is already closed
                                Console.WriteLine(
                                    $"The connection closed, status code:{_socket.CloseStatus},description: {_socket.CloseStatusDescription}, e:{e}");
                            }

                            break;
                        }

                        await ms.WriteAsync(buffer.Slice(0, receiveResult.Count));

                        if (receiveResult.EndOfMessage)
                        {
                            var str = Encoding.UTF8.GetString(ms.ToArray());
                            HandleMessage(str);
                            ms.SetLength(0);
                        }
                    }
                }
            }
            finally
            {
                if (!disableReconnection)
                {
                    _ = Task.Run(() => TryRecover());
                }
                else
                {
                    OnClosed();
                }
            }

            void HandleMessage(string str)
            {
                try
                {
                    var obj = JObject.Parse(str);
                    if (obj.TryGetValue("type", out var type) &&
                        string.Equals(type.Value<string>(), "system", StringComparison.OrdinalIgnoreCase) &&
                        obj.TryGetValue("event", out var @event) &&
                        string.Equals(@event.Value<string>(), "connected", StringComparison.OrdinalIgnoreCase))
                    {
                        // handle connected event
                        var connected = obj.ToObject<ConnectedMessage>();
                        if (!string.IsNullOrEmpty(connected?.connectionId))
                        {
                            _connectionId = connected.connectionId;
                        }
                        if (!string.IsNullOrEmpty(connected?.reconnectionToken))
                        {
                            _reconnectionToken = connected.reconnectionToken;
                        }
                    }
                    else
                    {
                        var response = JsonConvert.DeserializeObject<MessageResponse>(str);

                        if (response.sequenceId != null)
                        {
                            if (!_sequenceId.UpdateSequenceId(response.sequenceId.Value))
                            {
                                // duplicated message
                                return;
                            }
                        }

                        if (response.data != null)
                        {
                            var data = JsonConvert.DeserializeObject<RawWebsocketData>(response.data);
                            OnMessage?.Invoke(data.Ticks, data.Payload);
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }

            }
        }

        private async Task TryRecover()
        {
            if (!string.IsNullOrEmpty(_connectionId) && !string.IsNullOrEmpty(_reconnectionToken))
            {
                Console.WriteLine($"{_connectionId} is trying recovery");
                var url = QueryHelpers.AddQueryString(_baseUrl, new Dictionary<string, string> { [WebPubSubConnectionIdKey] = _connectionId, [ReconnectionTokenKey] = _reconnectionToken });
                var cts = new CancellationTokenSource(30 * 1000); //30s
                while (!cts.IsCancellationRequested)
                {
                    try
                    {
                        await ConnectAsyncCore(new Uri(url), default);
                        Console.WriteLine($"{_connectionId} is recovered");
                        return;
                    }
                    catch(Exception e)
                    {
                        Console.WriteLine(e.ToString());
                        await Task.Delay(1000);
                    }
                }

                Console.WriteLine("Recovery exceed timeout");
            }

            OnClosed();
        }

        private void OnClosed()
        {
            ConnectionState = State.Closed;
            OnClose?.Invoke();
        }

        private sealed class SequenceId
        {
            private readonly object _lock = new object();

            private ulong _sequenceId = 0;

            private volatile bool _updated;

            public ulong CurrentSequenceId => _sequenceId;

            public bool UpdateSequenceId(ulong sequenceId)
            {
                lock (_lock)
                {
                    _updated = true;

                    if (sequenceId > _sequenceId)
                    {
                        _sequenceId = sequenceId;
                        return true;
                    }

                    return false;
                }
            }

            public bool TryGetSequenceId(out ulong sequenceId)
            {
                lock (_lock)
                {
                    if (_updated)
                    {
                        sequenceId = _sequenceId;
                        _updated = false;
                        return true;
                    }

                    sequenceId = 0;
                    return false;
                }
            }
        }

        private sealed class SequenceAck
        {
            public string type = "sequenceAck";
            public ulong sequenceId;

            public SequenceAck(ulong sequenceId)
            {
                this.sequenceId = sequenceId;
            }

            public string Serialize()
            {
                return JsonConvert.SerializeObject(this, new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                });
            }
        }

        private class ConnectedMessage
        {
            public string type;
            public string @event;
            public string connectionId;
            public string reconnectionToken;
        }

        private sealed class MessageResponse
        {
            public string type;
            public string from;
            public string data;
            public ulong? sequenceId;

            public string Serialize()
            {
                return JsonConvert.SerializeObject(this, new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                });
            }
        }

        public enum State
        {
            NotStart = 0,
            Connected = 1,
            Closed = 2,
        }
    }
}

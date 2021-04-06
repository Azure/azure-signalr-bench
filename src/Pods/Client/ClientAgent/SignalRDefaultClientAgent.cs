// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.SignalRBench.Common;
using Azure.SignalRBench.Messages;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;

namespace Azure.SignalRBench.Client
{
    public class SignalRDefaultClientAgent : SignalRClientAgentBase
    {
        public SignalRDefaultClientAgent(string urlWithHub, Protocol protocol, string? userName, string[] groups, int globalIndex,
            ClientAgentContext context) : base(urlWithHub, protocol, userName, groups, globalIndex, context)
        {
        }

        public override Task EchoAsync(string payload) =>
            Connection.SendAsync("Echo", ClientAgentContext.CoordinatedUtcNow(), payload);

        public override async Task SendToClientAsync(int index, string payload)
        {
            var connectionID = await Context.GetConnectionIDAsync(index);
            await Connection.SendAsync("SendToConnection", connectionID, ClientAgentContext.CoordinatedUtcNow(), payload);
        }

        public override Task BroadcastAsync(string payload) =>
            Connection.SendAsync("Broadcast", ClientAgentContext.CoordinatedUtcNow(), payload);

        public override Task GroupBroadcastAsync(string group, string payload) =>
            Connection.SendAsync("GroupBroadcast", group, ClientAgentContext.CoordinatedUtcNow(), payload);

        public override Task JoinGroupAsync() => Task.WhenAll(Groups.Select(g => Connection.InvokeAsync("JoinGroup", g)));
    }
}
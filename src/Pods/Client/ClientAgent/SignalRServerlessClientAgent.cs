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
    public class SignalRServerlessClientAgent : SignalRClientAgentBase
    {
        public SignalRServerlessClientAgent(string url, Protocol protocol, string? userName, string[] groups,
            int globalIndex,
            ClientAgentContext context) : base(url, protocol, userName, groups, globalIndex, context)
        {
        }

        public async override Task EchoAsync(string payload) 
        {
          await  Connection.SendAsync("user", $"user{GlobalIndex}", DateTime.UtcNow.Ticks, payload);
        }

        public override async Task SendToClientAsync(int globalIndex, string payload)
        {
            await Connection.SendAsync("user", $"user{globalIndex}", DateTime.UtcNow.Ticks, payload);
        }

        public override Task BroadcastAsync(string payload) =>
            Connection.SendAsync("broadcast", DateTime.UtcNow.Ticks, payload);

        public override Task GroupBroadcastAsync(string group, string payload) =>
            Connection.SendAsync("group", group, DateTime.UtcNow.Ticks, payload);

        public override Task JoinGroupAsync() => Task.WhenAll(Groups.Select(g => Connection.InvokeAsync("add", g)));
    }
}
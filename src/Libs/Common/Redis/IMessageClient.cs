// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

namespace Azure.SignalRBench.Messages
{
    public interface IMessageClient
    {
        Task AckAsync(CommandMessage message, AckStatus status, string? error = null, double? progress = null);
        CommandMessage PrepareCommand(CommandMessage message);
        Task PublishPreparedCommandAsync(string target, CommandMessage message);
        Task<IDisposable> WithHandlers(params MessageHandler[] handlers);
        Task SendCommandAsync(string target, CommandMessage message);
    }
}
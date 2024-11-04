// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace Azure.SignalRBench.Messages
{
    public abstract class MessageHandler
    {
        public abstract string? Role { get; }

        public abstract string Command { get; }

        public abstract MessageType Type { get; }

        public abstract Task Handle(string text);

        public static MessageHandler CreateCommandHandler(string? role, string command, Func<CommandMessage, Task> handler) =>
            new CommandHandler(role, command, handler);

        public static MessageHandler CreateCommandHandler(string command, Func<CommandMessage, Task> handler) =>
            new CommandHandler(null, command, handler);

        public static MessageHandler CreateAckHandler(string command, Func<AckMessage, Task> handler) =>
            new AckHandler(command, handler);

        private sealed class CommandHandler : MessageHandler
        {
            private readonly Func<CommandMessage, Task> _handler;

            public CommandHandler(string? role, string name, Func<CommandMessage, Task> handler)
            {
                Role = role;
                Command = name;
                _handler = handler;
            }

            public override string? Role { get; }

            public override string Command { get; }

            public override MessageType Type => MessageType.Command;

            public override Task Handle(string text) => _handler(JsonConvert.DeserializeObject<CommandMessage>(text));
        }

        private sealed class AckHandler : MessageHandler
        {
            private readonly Func<AckMessage, Task> _handler;

            public AckHandler(string name, Func<AckMessage, Task> handler)
            {
                Command = name;
                _handler = handler;
            }

            public override string? Role => null;

            public override string Command { get; }

            public override MessageType Type => MessageType.Ack;

            public override Task Handle(string text) => _handler(JsonConvert.DeserializeObject<AckMessage>(text));
        }
    }
}

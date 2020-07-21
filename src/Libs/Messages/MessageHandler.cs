// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace Microsoft.Azure.SignalRBench.Messages
{
    public abstract class MessageHandler
    {
        public abstract string Name { get; }

        public abstract MessageType Type { get; }

        public abstract Task Handle(string text);

        public static MessageHandler CreateCommandHandler(string name, Func<CommandMessage, Task> handler) =>
            new CommandHandler(
                name ?? throw new ArgumentNullException(nameof(name)),
                handler ?? throw new ArgumentNullException(nameof(handler)));

        public static MessageHandler CreateAckHandler(string name, Func<AckMessage, Task> handler) =>
            new AckHandler(
                name ?? throw new ArgumentNullException(nameof(name)),
                handler ?? throw new ArgumentNullException(nameof(handler)));

        private sealed class CommandHandler : MessageHandler
        {
            private readonly Func<CommandMessage, Task> _handler;

            public CommandHandler(string name, Func<CommandMessage, Task> handler)
            {
                Name = name;
                _handler = handler;
            }

            public override string Name { get; }

            public override MessageType Type => MessageType.Command;

            public override Task Handle(string text) => _handler(JsonConvert.DeserializeObject<CommandMessage>(text));
        }

        private sealed class AckHandler : MessageHandler
        {
            private readonly Func<AckMessage, Task> _handler;

            public AckHandler(string name, Func<AckMessage, Task> handler)
            {
                Name = name;
                _handler = handler;
            }

            public override string Name { get; }

            public override MessageType Type => MessageType.Ack;

            public override Task Handle(string text) => _handler(JsonConvert.DeserializeObject<AckMessage>(text));
        }
    }
}

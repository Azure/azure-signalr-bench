// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Azure.SignalRBench.Storage
{
    public class QueueMessage<T>
    {
        internal QueueMessage(string messageId, string popReceipt, T value)
        {
            MessageId = messageId;
            PopReceipt = popReceipt;
            Value = value;
        }

        public string MessageId { get; }

        public string PopReceipt { get; internal set; }

        public T Value { get; }
    }
}

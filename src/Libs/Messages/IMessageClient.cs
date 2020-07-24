// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;

using Newtonsoft.Json.Linq;

namespace Azure.SignalRBench.Messages
{
    public interface IMessageClient
    {
        Task AckAsync(string target, int ackId, bool isCompleted, double? progress = null);
        Task<int> SendCommandAsync(string target, string command, JObject parameters = null);
    }
}
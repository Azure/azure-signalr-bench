// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.SignalR;

namespace Azure.SignalRBench.Coordinator
{
    public class SignalRProvider
    {
        private ISignalROperations? _signalROperations;

        public void Initialize(AzureCredentials credentials, string subscription)
        {
            var signalrManagementClient = new SignalRManagementClient(credentials);
            signalrManagementClient.SubscriptionId = subscription;
            _signalROperations = signalrManagementClient.SignalR;
        }

        private ISignalROperations SignalROperations => _signalROperations ?? throw new InvalidOperationException();
    }
}

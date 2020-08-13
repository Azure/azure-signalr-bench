// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Management.SignalR;

namespace Coordinator.SignalR
{
    internal class SignalRHelper
    {
        private readonly ISignalROperations _signalROperations;
        private readonly ISignalROperations _signalRPPEOperations;

        public SignalRHelper()
        {
            _signalROperations = GetSignalROperations();
            // Deal with this part later
            //  signalRPPEOperations = getSignalRPPEOperations();
        }

        private ISignalROperations GetSignalROperations()
        {
            var signalrManagementClient = new SignalRManagementClient(PerfConfig.ServicePrincipal);
            signalrManagementClient.SubscriptionId = PerfConfig.Subscription;
            return signalrManagementClient.SignalR;
        }

        private ISignalROperations GetSignalRPPEOperations()
        {
            var signalrManagementClient = new SignalRManagementClient(PerfConfig.PPE.ServicePrincipal);
            signalrManagementClient.SubscriptionId = PerfConfig.PPE.Subscription;
            return signalrManagementClient.SignalR;
        }
    }
}

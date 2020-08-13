// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Management.SignalR;

namespace Coordinator.SignalR
{
    internal class SignalRHelper
    {
        private ISignalROperations signalROperations;
        private ISignalROperations signalRPPEOperations;

        public SignalRHelper()
        {
            signalROperations = getSignalROperations();
            // Deal with this part later
            //  signalRPPEOperations = getSignalRPPEOperations();
        }

        private ISignalROperations getSignalROperations()
        {
            var signalrManagementClient = new SignalRManagementClient(PerfConfig.ServicePrincipal);
            signalrManagementClient.SubscriptionId = PerfConfig.Subscription;
            return signalrManagementClient.SignalR;
        }

        private ISignalROperations getSignalRPPEOperations()
        {
            var signalrManagementClient = new SignalRManagementClient(PerfConfig.PPE.ServicePrincipal);
            signalrManagementClient.SubscriptionId = PerfConfig.PPE.Subscription;
            return signalrManagementClient.SignalR;
        }
    }
}

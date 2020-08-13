using Microsoft.Azure.Management.SignalR;

namespace Coordinator.SignalR
{
    class SignalRHelper
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

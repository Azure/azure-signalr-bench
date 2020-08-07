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
            var signalrManagementClient = new SignalRManagementClient(PerfConfig.SERVICE_PRINCIPAL);
            signalrManagementClient.SubscriptionId = PerfConfig.SUBSCRIPTION;
            return signalrManagementClient.SignalR;
        }
        private ISignalROperations getSignalRPPEOperations()
        {
            var signalrManagementClient = new SignalRManagementClient(PerfConfig.PPE.SERVICE_PRINCIPAL);
            signalrManagementClient.SubscriptionId = PerfConfig.PPE.SUBSCRIPTION;
            return signalrManagementClient.SignalR;
        }
    }
}

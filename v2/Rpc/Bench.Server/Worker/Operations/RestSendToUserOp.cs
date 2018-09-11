using Bench.RpcSlave.Worker.Serverless;

namespace Bench.RpcSlave.Worker.Operations
{
    class RestSendToUserOp : RestSendMsgOp
    {
        /// <summary>
        /// Generate REST API url for sendToUser.
        /// </summary>
        /// <param name="ServiceUtils">It has connectionString and HubName.</param>
        /// <param name="userId">userId</param>
        public override string GenRestUrl(ServiceUtils serviceUtils, string userId)
        {
            return serviceUtils.GetSendToUserUrl(ServiceUtils.HubName, userId);
        }
    }
}

using Bench.RpcSlave.Worker.Serverless;

namespace Bench.RpcSlave.Worker.Operations
{
    class RestSendToUserOp : RestSendMsgOp
    {
        /// <summary>
        /// Generate REST API url for sendToUser.
        /// </summary>
        /// <param name="ServiceUtils">It has connectionString and HubName.</param>
        /// <param name="userIdPostfix">It is used to create the userId.</param>
        public override string GenRestUrl(ServiceUtils serviceUtils, string userIdPostfix)
        {
            return serviceUtils.GetSendToUserUrl(ServiceUtils.HubName,
                $"{ServiceUtils.ClientUserIdPrefix}{userIdPostfix}");
        }
    }
}

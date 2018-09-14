using Bench.RpcSlave.Worker.Serverless;

namespace Bench.RpcSlave.Worker.Operations
{
    class RestBroadcastOp : RestSendMsgOp
    {
        public override string GenRestUrl(ServiceUtils serviceUtils, string arg)
        {
            return serviceUtils.GetBroadcastUrl();
        }
    }
}

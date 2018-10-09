using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Grpc.Core;

namespace Rpc.Service
{
    public class RpcServiceImpl: RpcService.RpcServiceBase
    {
        public override Task<Empty> Update(Data data, ServerCallContext context)
        {
            throw new NotImplementedException();
        }

        public override Task<Data> Query(Data data, ServerCallContext context)
        {
            throw new NotImplementedException();
        }

    }
}

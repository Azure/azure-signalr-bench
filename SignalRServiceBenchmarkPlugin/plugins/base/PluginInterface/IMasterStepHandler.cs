using Rpc.Service;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Plugin.Base
{
    public interface IMasterStepHandler
    {
        // In a master step, the Rpc operations may be Query, Update, etc. 
        Task HandleMasterStep(MasterStep step, IList<IRpcClient> rpcClients);
    }
}

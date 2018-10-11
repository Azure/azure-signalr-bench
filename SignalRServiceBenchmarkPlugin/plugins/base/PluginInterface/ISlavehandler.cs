using Rpc.Service;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Plugin.Base
{
    public interface ISlavehandler
    {
        Task HandleSlaveStep(IDictionary<string, object> parameters);
    }
}

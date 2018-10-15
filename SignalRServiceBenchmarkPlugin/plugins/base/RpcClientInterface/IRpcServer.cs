using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Rpc.Service
{
    public interface IRpcServer
    {
        IRpcServer Create(string hostname, int port);
        Task Start();
    }
}

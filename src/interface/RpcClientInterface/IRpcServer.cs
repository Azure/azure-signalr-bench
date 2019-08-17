using System.Threading.Tasks;

namespace Rpc.Service
{
    public interface IRpcServer
    {
        IRpcServer Create(string hostname, int port);

        Task Start();

        Task Stop();
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public interface ISemaphoreTokenBucket : IDisposable
    {
        Task WaitAsync();

        long Release();
    }
}

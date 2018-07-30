using System;
using System.Collections.Generic;
using System.Text;

namespace Bench.RpcMaster.Allocators
{
    class OneAllocator : IAllocator
    {
        public Dictionary<string, Dictionary<string, int>> Allocate(List<string> slaves, int totalConn, Dictionary<string, int> criteria)
        {
            // TODO: only for dev
            Dictionary<string, Dictionary<string, int>> result = new Dictionary<string, Dictionary<string, int>>();

            Dictionary<string, int> all = new Dictionary<string, int>();
            all["echo"] = totalConn;
            result[slaves[0]] = all;

            return result;
        }
    }
}

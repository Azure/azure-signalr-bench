using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Rpc.Service
{
    public static class RpcUtil
    {
        public static Dictionary<string, object> Deserialize(string input)
        {
            try
            {
                var parameters = JsonConvert.DeserializeObject<Dictionary<string, object>>(input);
                return parameters;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public static string Serialize(IDictionary<string, object> data)
        {
            var json = JsonConvert.SerializeObject(data);
            return json;
        }
    }
}

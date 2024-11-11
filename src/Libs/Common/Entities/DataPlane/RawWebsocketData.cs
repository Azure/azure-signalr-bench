using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Azure.SignalRBench.Common
{
    public class RawWebsocketData
    {
        public string Type { get; set; }= "";

        public string Target { get; set; } = "";
        public string Payload { get; set; } = "";
        public long Ticks { get; set; }
        public string Serilize()
        {
            return JsonConvert.SerializeObject(this, new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });
        }
    }
}
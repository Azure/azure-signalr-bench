using System;
using System.Collections.Generic;
using System.Text;
using BasePlugin;
using Newtonsoft.Json;
using YamlDotNet.RepresentationModel;

namespace Microsoft.Azure.SignalR.Benchmark.DataModule
{
    public class SampleStep : BaseStep
    {
        public override bool Deserialize(IList<string> input)
        {
            try
            {
                Parameters = JsonConvert.DeserializeObject<Dictionary<string, object>>(input[0]);
            }
            catch(Exception ex)
            {
                return false;
            }
            return true;
        }

        public override IList<string> Serialize()
        {
            var json = JsonConvert.SerializeObject(Parameters);
            var list = new List<string>();
            list.Add(json);
            return list;
        }

        protected override bool Validate(YamlNode stepNode)
        {
            return true;
        }
    }
}

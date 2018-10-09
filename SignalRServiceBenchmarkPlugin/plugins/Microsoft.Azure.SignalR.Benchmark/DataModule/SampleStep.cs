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
        public override bool Deserialize(string input)
        {
            try
            {
                Parameters = JsonConvert.DeserializeObject<Dictionary<string, object>>(input);
            }
            catch(Exception ex)
            {
                return false;
            }
            return true;
        }

        public override string Serialize()
        {
            var json = JsonConvert.SerializeObject(Parameters);
            return json;
        }

        protected override bool Validate(YamlNode stepNode)
        {
            return true;
        }
    }
}

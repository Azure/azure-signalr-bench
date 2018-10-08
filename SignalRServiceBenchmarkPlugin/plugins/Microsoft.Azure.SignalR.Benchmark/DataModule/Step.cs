using System;
using System.Collections.Generic;
using System.Text;
using BasePlugin;
using Newtonsoft.Json;
using YamlDotNet.RepresentationModel;

namespace Microsoft.Azure.SignalR.Benchmark.DataModule
{
    class Step : BaseStep
    {
        public override bool Deserialize(IList<string> input)
        {
            try
            {
                IntegerDictionary = JsonConvert.DeserializeObject<Dictionary<string, int>>(input[0]);
            }
            catch(Exception ex)
            {
                return false;
            }
            return true;
        }

        public override IList<string> Serialize()
        {
            var json = JsonConvert.SerializeObject(IntegerDictionary);
            var list = new List<string>();
            return list;
        }



        protected override bool ParseParameter(YamlMappingNode stepNode)
        {
            var success = true;
            foreach (var entry in stepNode)
            {
                var parameterName = ((YamlScalarNode)entry.Key).Value;
                
                if (parameterName == TypeKey)
                {
                    Type = parameterName;
                    continue;
                }
                var parameterValue = Convert.ToInt32(entry.Value.ToString());
                success = IntegerDictionary.TryAdd(parameterName, parameterValue);
                if (!success) return success;
            }
            return success;
        }

        protected override bool Validate(YamlNode stepNode)
        {
            return true;
        }
    }
}

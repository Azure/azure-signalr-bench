using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using YamlDotNet.RepresentationModel;

namespace Plugin.Base
{
    public class MasterStep
    {
        public IDictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();

        protected readonly string TypeKey = "Type";
        protected readonly string MethodKey = "Method";

        public bool Parse(YamlMappingNode stepNode)
        {
            var success = true;

            success = Validate(stepNode);
            if (!success) return success;

            foreach (var entry in stepNode)
            {
                var parameterName = ((YamlScalarNode)entry.Key).Value;
                var parameterValue = ((YamlScalarNode)entry.Value).Value;
                success = Parameters.TryAdd(parameterName, parameterValue);
                if (!success) return success;
            }
            return success;
        }

        // TODO: validate more on Step
        protected bool Validate(YamlMappingNode stepNode)
        {
            var keys = stepNode.Children.Keys;
            if (!keys.Contains(new YamlScalarNode(TypeKey))) return false;
            if (!keys.Contains(new YamlScalarNode(MethodKey))) return false;
            return true;
        }

        // TODO: Deserialize should be in SlaveStep
        //public bool Deserialize(string input)
        //{
        //    try
        //    {
        //        Parameters = JsonConvert.DeserializeObject<Dictionary<string, object>>(input);
        //    }
        //    catch (Exception ex)
        //    {
        //        return false;
        //    }
        //    return true;
        //}

        public string Serialize()
        {
            var json = JsonConvert.SerializeObject(Parameters);
            return json;
        }
    }
}

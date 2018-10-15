using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using YamlDotNet.RepresentationModel;

namespace Plugin.Base
{
    public class MasterStep
    {
        public IDictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
        
        public string GetMethod()
        {
            try
            {
                Parameters.TryGetValue(Constants.Method, out object method);
                return (string)method;
            }
            catch (Exception ex)
            {
                var message = $"Method name does not exists.";
                Log.Error(message);
                throw new Exception(message);
            }
        }

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
            if (!keys.Contains(new YamlScalarNode(Constants.Type))) return false;
            if (!keys.Contains(new YamlScalarNode(Constants.Method))) return false;
            return true;
        }
    }
}

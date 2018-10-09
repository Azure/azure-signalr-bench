using System;
using System.Collections.Generic;
using System.Text;
using YamlDotNet.RepresentationModel;

namespace BasePlugin
{
    public abstract class BaseStep
    {
        public IDictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
        protected readonly string TypeKey = "Type";
        protected readonly string MethodKey = "Method";

        public abstract bool Deserialize(string input);

        public abstract string Serialize();

        public bool Parse(YamlMappingNode stepNode)
        {
            var success = true;

            success = InternalValidate(stepNode);
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

        protected bool InternalValidate(YamlMappingNode stepNode)
        {
            var keys = stepNode.Children.Keys;
            if (!keys.Contains(new YamlScalarNode(TypeKey))) return false;
            if (!keys.Contains(new YamlScalarNode(MethodKey))) return false;
            return Validate(stepNode);
        }

        protected abstract bool Validate(YamlNode stepNode);
    }
}

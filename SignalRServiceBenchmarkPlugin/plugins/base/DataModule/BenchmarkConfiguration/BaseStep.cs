using System;
using System.Collections.Generic;
using System.Text;
using YamlDotNet.RepresentationModel;

namespace BasePlugin
{
    public abstract class BaseStep
    {
        public IDictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
        public string Type { get; set; }
        public string Method { get; set; }
        protected readonly string TypeKey = "Type";
        protected readonly string MethodKey = "Method";

        public abstract bool Deserialize(IList<string> input);

        public abstract IList<string> Serialize();

        protected bool Parse(YamlMappingNode stepNode)
        {
            var success = true;

            success = InternalValidate(stepNode);
            if (!success) return success;

            foreach (var entry in stepNode)
            {
                var parameterName = ((YamlScalarNode)entry.Key).Value;

                if (parameterName == TypeKey)
                {
                    Type = parameterName;
                    continue;
                }
                var parameterValue = Convert.ToInt32(entry.Value.ToString());
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

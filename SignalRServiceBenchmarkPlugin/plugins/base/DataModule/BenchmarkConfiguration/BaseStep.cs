using System;
using System.Collections.Generic;
using System.Text;
using YamlDotNet.RepresentationModel;

namespace BasePlugin
{
    public abstract class BaseStep
    {
        public IDictionary<string, int> IntegerDictionary { get; set; } = new Dictionary<string, int>();
        public IDictionary<string, string> StringDictionary { get; set; } = new Dictionary<string, string>();
        public IDictionary<string, Tuple<int, int>> IntegerPairDictionary { get; set; } = new Dictionary<string, Tuple<int, int>>();
        public IDictionary<string, double> doubleDictionary { get; set; } = new Dictionary<string, double>();
        public string Type { get; set; }
        public string Method { get; set; }
        protected readonly string TypeKey = "Type";
        protected readonly string MethodKey = "Method";
        protected readonly string ParameterKey = "Parameter";

        public abstract bool Deserialize(IList<string> input);

        public abstract IList<string> Serialize();

        public bool Parse(YamlMappingNode stepNode)
        {
            var success = true;

            success = Validate(stepNode);
            if (!success) return success;

            // Get step type
            Type = stepNode.Children[new YamlScalarNode(TypeKey)].ToString();

            // Get step method
            Method = stepNode.Children[new YamlScalarNode(MethodKey)].ToString();

            // Get step pamameters
            ParseParameter((YamlMappingNode)stepNode.Children[new YamlScalarNode(ParameterKey)]);

            return success;
            
        }

        protected abstract bool ParseParameter(YamlMappingNode input);
        protected abstract bool Validate(YamlNode stepNode);
    }
}

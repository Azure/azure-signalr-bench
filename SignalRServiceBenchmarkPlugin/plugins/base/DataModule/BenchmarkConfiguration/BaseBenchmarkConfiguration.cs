using System;
using System.Collections.Generic;
using YamlDotNet.RepresentationModel;

namespace BasePlugin
{
    public abstract class BaseBenchmarkConfiguration
    {
        protected static readonly string ModuleNameKey = "ModuleName";
        protected static readonly string PipelineKey = "Pipeline";
        protected static readonly string TypesKey = "Types";

        public string ModuleName {get; set; }
        public IList<string> Types { get; set; }
        public IList<IList<BaseStep>> Pipeline { get; set; }
        protected abstract bool ValidateCore(YamlMappingNode root);
        public abstract bool Parse(string root);

        public bool Validate(YamlMappingNode root)
        {
            var success = true;
            var keys = root.Children.Keys;
            success = keys.Contains(new YamlScalarNode(ModuleNameKey));
            if (!success) return success;
            success = keys.Contains(new YamlScalarNode(PipelineKey));
            if (!success) return success;
            success = keys.Contains(new YamlScalarNode(TypesKey));
            if (!success) return success;
            return ValidateCore(root);

        }
    }
}

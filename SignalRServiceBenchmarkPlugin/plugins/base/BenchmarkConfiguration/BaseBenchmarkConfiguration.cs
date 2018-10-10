using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using YamlDotNet.RepresentationModel;

namespace Plugin.Base
{
    public class BenchmarkConfiguration
    {
        protected static readonly string ModuleNameKey = "ModuleName";
        protected static readonly string PipelineKey = "Pipeline";
        protected static readonly string TypesKey = "Types";

        public string ModuleName {get; set; }
        public IList<string> Types { get; set; } = new List<string>();
        public IList<IList<MasterStep>> Pipeline { get; set; } = new List<IList<MasterStep>>();

        public BenchmarkConfiguration(string content)
        {
            Parse(content);
        }

        protected void Parse(string content)
        {
            // Setup input
            var input = new StringReader(content);

            // Load the stream
            var yaml = new YamlStream();
            yaml.Load(input);

            // validate the stream
            YamlMappingNode mapping = null;
            try
            {
                mapping = (YamlMappingNode)yaml.Documents[0].RootNode;
                ValidateBenchmarkConfiguration(mapping);
            }
            catch (Exception ex)
            {
                Log.Error($"Benchmark configuration is invalid: {ex}");
                throw new Exception("Benchmark configuration is invalid.");

            }

            // Parse the stream
            // Parse module name
            ModuleName = (mapping.Children[new YamlScalarNode(ModuleNameKey)] as YamlScalarNode).Value;

            // Parse types
            var types = mapping.Children[new YamlScalarNode(TypesKey)] as YamlSequenceNode;
            foreach (var type in types)
            {
                Types.Add(type.ToString());
            }

            // Parse pipeline
            var pipelineNode = (YamlSequenceNode)mapping.Children[new YamlScalarNode(PipelineKey)] as YamlSequenceNode;
            foreach (var parallelStepNode in pipelineNode)
            {
                var parallelSteps = new List<MasterStep>();
                foreach (var stepNode in (YamlSequenceNode)parallelStepNode)
                {
                    var step = new MasterStep();
                    step.Parse((YamlMappingNode)stepNode);
                    parallelSteps.Add(step);
                }
                Pipeline.Add(parallelSteps);

            }

            return;
        }

        public void ValidateBenchmarkConfiguration(YamlMappingNode root)
        {
            var success = true;
            var keys = root.Children.Keys;
            success = keys.Contains(new YamlScalarNode(ModuleNameKey));
            if (!success)
            {
                Log.Error($"Module name is required, but not found.");
                throw new Exception($"Module name is required, but not found.");
            }
            success = keys.Contains(new YamlScalarNode(PipelineKey));
            if (!success)
            {
                Log.Error($"Pipeline is required, but not found.");
                throw new Exception($"Pipeline is required, but not found.");
            }
            success = keys.Contains(new YamlScalarNode(TypesKey));
            if (!success)
            {
                Log.Error($"Types is required, but not found.");
                throw new Exception($"Types is required, but not found.");
            }
        }
    }
}

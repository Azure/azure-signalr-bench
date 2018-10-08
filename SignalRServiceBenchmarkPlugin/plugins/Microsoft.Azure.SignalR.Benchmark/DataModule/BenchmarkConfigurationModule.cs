using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using YamlDotNet.RepresentationModel;
using BasePlugin;

namespace Microsoft.Azure.SignalR.Benchmark.DataModule
{
    public class BenchmarkConfigurationModule: BaseBenchmarkConfiguration
    {
        public BenchmarkConfigurationModule()
        {
            Types = new List<string>();
            Pipeline = new List<IList<BaseStep>>();
        }


        public override bool Parse(string content)
        {
            // Setup input
            var input = new StringReader(content);

            // Load the stream
            var yaml = new YamlStream();
            yaml.Load(input);

            // validate the stream
            var mapping = (YamlMappingNode)yaml.Documents[0].RootNode;
            var success = Validate(mapping);

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
            foreach(var parallelStepNode in pipelineNode)
            {
                var parallelSteps = new List<BaseStep>();
                foreach(var stepNode in (YamlSequenceNode)parallelStepNode)
                {
                    var step = new Step();
                    step.Parse((YamlMappingNode)stepNode);
                    parallelSteps.Add(step);
                }
                Pipeline.Add(parallelSteps);

            }

            return success;
        }

        protected override bool ValidateCore(YamlMappingNode mapping)
        {
            return true;
        }

    }
}

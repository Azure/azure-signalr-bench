using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using YamlDotNet.RepresentationModel;

namespace Microsoft.Azure.SignalR.Benchmark.DataModule
{
    public class BenchmarkConfigurationModule
    {
        private static readonly string ModuleNameKey = "ModuleName";
        private static readonly string PipelineKey = "Pipeline";
        private static readonly string TypesKey = "Types";

        private IValidator _validator;

        public string ModuleName { get; set; }
        private IList<string> _types;

        public BenchmarkConfigurationModule()
        {
        }

        //List<StepConfigurationModule> _pipeline;

        public BenchmarkConfigurationModule(IValidator validator)
        {
            _validator = validator;
        }

        public void Parse(string content)
        {
            // Setup input
            var input = new StringReader(content);

            // Load the stream
            var yaml = new YamlStream();
            yaml.Load(input);

            // validate the stream
            var mapping = (YamlMappingNode)yaml.Documents[0].RootNode;
            var success = _validator.Validate(mapping);

            // Parse the stream
            ModuleName = (mapping.Children[new YamlScalarNode(ModuleNameKey)] as YamlScalarNode).Value;
            //var pipeline = (YamlSequenceNode)mapping.Children[new YamlSequenceNode(Pipeline)];

        }


        /* TODO: Make interface and move to other file*/

        // validator
        public interface IValidator
        {
            bool Validate(YamlMappingNode root);
        }

        public class Validator
        {
            public bool Validate()
            {
                throw new NotImplementedException();
            }
        }
    }
}

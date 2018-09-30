using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using YamlDotNet.RepresentationModel;

namespace Microsoft.Azure.SignalR.Benchmark.DataModule
{
    public class BenchmarkConfigurationModule
    {
        private static readonly string ModuleName = "ModuleName";
        private static readonly string Pipeline = "Pipeline";
        private static readonly string Types = "Types";

        private IValidator _validator;

        private string _moduleName;
        private IList<string> _types;
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
            _moduleName = (mapping.Children[new YamlScalarNode(ModuleName)] as YamlScalarNode).Value;
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

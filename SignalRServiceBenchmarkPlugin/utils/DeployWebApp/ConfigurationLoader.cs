using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace DeployWebApp
{
    public class ConfigurationLoader
    {
        public ConfigurationLoader()
        {

        }

        public T Load<T>(string path)
        {
            var content = ReadFile<T>(path);
            return Parse<T>(content);
        }

        private string ReadFile<T>(string path)
        {
            var content = File.ReadAllText(path);
            return content;
        }

        private T Parse<T>(string content)
        {
            var input = new StringReader(content);

            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(new CamelCaseNamingConvention())
                .Build();

            var config = deserializer.Deserialize<T>(input);

            return config;
        }
    }
}

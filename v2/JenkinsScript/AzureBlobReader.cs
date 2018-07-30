using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace JenkinsScript
{
    public class AzureBlobReader
    {
        public static string ReadBlob(string configBlobName)
        {
            // load signalr config
            CloudStorageAccount storageAccount = null;
            CloudBlobContainer cloudBlobContainer = null;
            var content = "";
            var AzureStorageConnectionString = Environment.GetEnvironmentVariable("AzureStorageConnectionString");
            Console.WriteLine($"AzureStorageConnectionString : {AzureStorageConnectionString }");
            Console.WriteLine($"container: {Environment.GetEnvironmentVariable("ConfigBlobContainerName")}");
            Console.WriteLine($"configkey: {configBlobName}");
            if (CloudStorageAccount.TryParse(AzureStorageConnectionString, out storageAccount))
            {
                try
                {
                    CloudBlobClient cloudBlobClient = storageAccount.CreateCloudBlobClient();
                    cloudBlobContainer = cloudBlobClient.GetContainerReference(Environment.GetEnvironmentVariable("ConfigBlobContainerName"));
                    CloudBlockBlob cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference(Environment.GetEnvironmentVariable(configBlobName));
                    content = cloudBlockBlob.DownloadTextAsync().GetAwaiter().GetResult();

                }
                catch (StorageException ex)
                {
                    Console.WriteLine("Error returned from the service: {0}", ex.Message);

                }
            }
            return content;
        }

        public static T ParseYaml<T>(string content)
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

using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Client.ClientJobNs;
using Client.WorkerNs;
using Client.UtilNs;
using Microsoft.AspNetCore.Http.Connections;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace Client
{
    public class Startup
    {
        private static HttpClient _httpClient;
        private static HttpClientHandler _httpClientHandler;

        private const string _defaultUrl = "http://localhost:5002";

        //public void ConfigureServices(IServiceCollection services)
        //{
        //    services.AddMvc();
        //}

        //public void Configure(IApplicationBuilder app)
        //{
        //    app.UseMvc();

        //    // Register a default startup page to ensure the application is up
        //    app.Run((context) =>
        //    {
        //        return context.Response.WriteAsync("Client is runing");
        //    });
        //}

        public static int Main(string[] args)
        {
            // TODO: to remove, only for debug 
            //ConfigureRepo();

            var app = new CommandLineApplication()
            {
                Name = "ASRSBenchmarkClient",
                FullName = "Azure SignalR Service Benchmark Client",
                Description = "REST APIs to run Azure SignalR service benchmark client",
                OptionsComparison = StringComparison.OrdinalIgnoreCase
            };

            app.HelpOption("-?|-h|--help");

            var urlOption = app.Option("-u|--url", $"URL for Rest APIs.  Default is '{_defaultUrl}'.", CommandOptionType.SingleValue);
            var jobsOptions = app.Option("-j|--jobs", "The path or url to the jobs definition.", CommandOptionType.SingleValue);
            

            app.OnExecute(() =>
            {
                var url = urlOption.HasValue() ? urlOption.Value() : _defaultUrl;
                Log($"url: {url}");

                // load client job
                var allJobs = LoadClientJobs(jobsOptions);
                Log($"job count: {allJobs.Count}");

                return Run(url, allJobs).Result;
            });

            // Configuring the http client to trust the self-signed certificate
            _httpClientHandler = new HttpClientHandler();
            _httpClientHandler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            _httpClient = new HttpClient(_httpClientHandler);

            return app.Execute(args);
        }

        private static async Task<int> Run(string url, List<ClientJob> allJobs)
        {
            //var host = new WebHostBuilder()
            //        .UseKestrel()
            //        .UseStartup<Startup>()
            //        .UseUrls(url)
            //        .Build();

            //var hostTask = host.RunAsync();

            var processJobsTask = ProcessJobs(allJobs);

            //var completedTask = await Task.WhenAny(hostTask, processJobsTask);
            var completedTask = await Task.WhenAny(processJobsTask);

            // Propagate exception (and exit process) if either task faulted
            await completedTask;

            // Host exited normally, so cancel job processor
            await processJobsTask;

            return 0;
        }

        private static async Task ProcessJobs(List<ClientJob> allJobs)
        {
            Log($"ProcessJobs");
            var whenLastJobCompleted = DateTime.MinValue;


            for (int i = 0; i < allJobs.Count(); i++)
            {
                var job = allJobs[i];
                Log($"jod ID: {job.Id}, State: {job.State}");

                // TODO: to remove, only for debug
                Log($"{job.State} {job.TransportType}");

                if (job.State == ClientState.Waiting)
                {
                    Log($"Starting SignalR worker");
                    job.State = ClientState.Starting;

                    try
                    {
                        BaseWorker worker = WorkerFactory.CreateWorker(job);

                        if (worker == null)
                        {
                            Log($"Error while creating the worker");
                            job.State = ClientState.Deleting;
                            whenLastJobCompleted = DateTime.UtcNow;
                        }
                        else
                        {
                            var processJobTask = worker.ProcessJobAsync();
                            Task.WaitAll(processJobTask);
                            Thread.Sleep(100);
                        }
                    }
                    catch (Exception e)
                    {
                        Log($"An unexpected error occured while starting the job {job.Id}");
                        Log(e.ToString());
                    }
                }
                
            }
        }

        private static List<ClientJob> LoadClientJobs(CommandOption jobsOptions)
        {
            JobDefinition jobDefinitions;
            List<ClientJob> allJobs = new List<ClientJob>();
            var jobDefinitionPathOrUrl = jobsOptions.Value();
            string jobDefinitionContent = "";

            // Load the job definition from a url or locally
            try
            {
                if (jobDefinitionPathOrUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    jobDefinitionContent = _httpClient.GetStringAsync(jobDefinitionPathOrUrl).GetAwaiter().GetResult();
                }
                else
                {
                    jobDefinitionContent = File.ReadAllText(jobDefinitionPathOrUrl);
                }
            }
            catch
            {
                Console.WriteLine($"Job definition '{jobDefinitionPathOrUrl}' could not be loaded.");
            }

            jobDefinitions = JsonConvert.DeserializeObject<JobDefinition>(jobDefinitionContent);

            if (!jobDefinitions.TryGetValue("Default", out var defaultJob))
            {
                defaultJob = new JObject();
            }


            foreach (var jobDef in jobDefinitions)
            {
                if (jobDef.Key == "Default") continue;
                var job = jobDef.Value;
                var mergedClientJob = new JObject(defaultJob);
                mergedClientJob.Merge(job);
                Log($"{mergedClientJob}");
                var clientJob = mergedClientJob.ToObject<ClientJob>();
                allJobs.Add(clientJob);
            }

            return allJobs;
        }

        private static void Log(string message)
        {
            var time = DateTime.Now.ToString("hh:mm:ss.fff");
            Util.ColorWriteLine($"[{time}] {message}");
        }
    }
}
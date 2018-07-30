using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Diagnostics;
using System.Threading;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Client.ClientJobNs;
using Client.UtilNs;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Connections;
using Client.StartTimeOffsetGenerator;
using Client.Statistics;
using Client.Statistics.Savers;
using Client.Tools;
using Client.Workers.OperationsNs;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.DependencyInjection;

namespace Client.WorkerNs
{
    public class BaseWorker
    {
        public string JobLogText { get; set; }
        // TODO: decouple
        protected HttpClientHandler _httpClientHandler;
        protected bool _stopped;
        protected SemaphoreSlim _lock = new SemaphoreSlim(1);

        protected BaseTool _pkg = new BaseTool();

        private readonly ILoggerFactory _loggerFactory;

        public BaseWorker(ClientJob job)
        {
            _pkg.Job = job;
        }

        int laterTime = 4;

        public async Task ProcessJobAsync()
        {
            // initial job
            InitializeJob();

            // start connections
            await StartConnections();


            // start jobs
            Task.Delay(10 * 1000).Wait();
            Util.Log("start jobs");
            StartJob();

            // stop job
            Util.Log($"wait {(_pkg.Job.Duration * 2 + _pkg.Job.Interval + laterTime) + 40}s to stop");
            var stopTask = Task.Delay(TimeSpan.FromSeconds((_pkg.Job.Duration + _pkg.Job.Interval + laterTime)  + 40)).ContinueWith(async _ =>
            {
                
                await StopJobAsync();
            });

            await Task.WhenAll(stopTask);
        }


        protected void StartJob()
        {
            string[] scenarios = _pkg.Job.Scenarios.Split(null);

            System.Timers.Timer timer = new System.Timers.Timer();
            timer.AutoReset = true;
            int ind = 0;
            timer.Elapsed += (sender, e) =>
            {
                // set new interval
                timer.Stop();
                timer.Interval = (_pkg.Job.Duration + laterTime) * 1000;
                timer.Start();

                if (ind >= scenarios.Length)
                {
                    timer.Stop();
                    return;
                }


                string scenario = scenarios[ind++];
                Util.Log($"scenario: {scenario}");
                EchoOp op = (EchoOp)OperationFactory.CreateOperation(scenario, _pkg);
                op.Setup();
                op.Process();
                Util.Log($"statistics delay time: {(_pkg.Job.Duration + laterTime / 2)}s");
                Task.Delay((_pkg.Job.Duration + laterTime / 2 + 5) * 1000 + 120 *1000).ContinueWith(_ =>
                {
                    Util.Log($"Show statistics");
                    op.SaveCounters();
                    Util.Log($"msg send: {op.totalSentMsg}, receive: {op.totalReceivedMsg}");

                });
            };
            timer.Start();

            
            
        }


        private async Task StopJobAsync()
        {
            Util.Log($"Stopping Job: {_pkg.Job.Id}");
            if (_stopped || !await _lock.WaitAsync(0))
            {
                // someone else is stopping, we only need to do it once
                return;
            }
            try
            {
                _stopped = true;

                // TODO: stop or dispose connections?
                var tasks = new List<Task>(_pkg.Connections.Count);
                foreach (var connection in _pkg.Connections)
                {
                    tasks.Add(connection.StopAsync());
                }
                await Task.WhenAll(tasks);
                var tasks2 = new List<Task>(_pkg.Connections.Count);
                foreach (var connection in _pkg.Connections)
                {
                    tasks2.Add(connection.DisposeAsync());
                }
                await Task.WhenAll(tasks2);
                Util.Log($"Stop connections");

            }
            finally
            {
                _lock.Release();
                _pkg.Job.State = ClientState.Completed;
            }
        }

        protected void InitializeJob()
        {
            _stopped = false;

            Debug.Assert(_pkg.Job.Connections > 0, "There must be more than 0 connections");

            // Configuring the http client to trust the self-signed certificate
            _httpClientHandler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
                MaxConnectionsPerServer = 200000,
                MaxAutomaticRedirections = 200000,
            };

            var jobLogText = $"[ID:{_pkg.Job.Id} Connections:{_pkg.Job.Connections} Duration:{_pkg.Job.Interval} ServerUrl:{_pkg.Job.ServerBenchmarkUri}";
            jobLogText += $" TransportType:{_pkg.Job.TransportType}";
            jobLogText += $" HubProtocol:{_pkg.Job.HubProtocol}";
            jobLogText += "]";

            Util.Log(jobLogText);

            if (_pkg.Connections == null)
            {
                CreateConnections();
            }
        }

        protected void CreateConnections(HttpTransportType transportType = HttpTransportType.WebSockets)
        {

            _pkg.Connections = new List<HubConnection>(_pkg.Job.Connections);
            _pkg.SentMassage = new List<int>(_pkg.Job.Connections);

            for (var i = 0; i < _pkg.Job.Connections; i++)
            {
                
                var hubConnectionBuilder = new HubConnectionBuilder()
                .WithUrl(_pkg.Job.ServerBenchmarkUri, transportType, httpConnectionOptions =>
                {
                    
                    httpConnectionOptions.HttpMessageHandlerFactory = _ => _httpClientHandler;
                    httpConnectionOptions.Transports = transportType;
                    httpConnectionOptions.CloseTimeout = TimeSpan.FromMinutes(100);
                    //httpConnectionOptions.SkipNegotiation = true;
                    //httpConnectionOptions.Url = new Uri(_pkg.Job.ServerBenchmarkUri);
                });

                //hubConnectionBuilder = hubConnectionBuilder.ConfigureLogging(builder => builder.SetMinimumLevel(LogLevel.Trace));

                HubConnection connection = null;
                switch (_pkg.Job.HubProtocol)
                {
                    case "json":
                        // json hub protocol is set by default
                        connection = hubConnectionBuilder.ConfigureLogging(builder => builder.SetMinimumLevel(LogLevel.Debug)).Build();
                        break;
                    case "messagepack":
                        connection = hubConnectionBuilder.AddMessagePackProtocol().ConfigureLogging(builder => builder.SetMinimumLevel(LogLevel.Trace)).Build();
                        break;
                    default:
                        throw new Exception($"{_pkg.Job.HubProtocol} is an invalid hub protocol name.");
                }

                
                _pkg.Connections.Add(connection);
                _pkg.SentMassage.Add(0);
                // Capture the connection ID
                var ind = i;

                connection.Closed += e =>
                {
                    if (!_stopped)
                    {
                        var error = $"{ind}th Connection closed early: {e}";
                        _pkg.Job.Error += Environment.NewLine + $"[{DateTime.Now.ToString("hh:mm:ss.fff")}] " + error;
                        Util.Log(error);
                    }

                    return Task.CompletedTask;
                };
            }
        }

        protected async Task StartConnections()
        {
            try
            {
                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();

                Util.Log($"Start connecting");
                //foreach (var connection in _pkg.Connections)
                //{
                //    connection.ServerTimeout = TimeSpan.FromMinutes(10);
                //}


                foreach (var connection in _pkg.Connections)
                {
                    connection.ServerTimeout = TimeSpan.FromMinutes(100);
                    connection.HandshakeTimeout = TimeSpan.FromMinutes(100);

                }


                /* connection method 1:*/
                var tasks = new List<Task>();

                for (var i = 0; i < _pkg.Connections.Count; i++)
                {
                    // TODO: bug in signal client
                    //tasks.Add((_pkg.Connections[i].StartAsync()));
                    //if (i > 0 && i % 100 == 0)
                    //{
                    //    Task.WhenAll(tasks).Wait();
                    //    Util.Log($"wait {i} connections start");
                    //}
                    int ind = i;
                    tasks.Add(Task.Delay(ind / 100 * 1000).ContinueWith(_ => _pkg.Connections[ind].StartAsync()));
                }

                // foreach (var conn in _pkg.Connections)
                // {
                //    tasks.Add(conn.StartAsync());
                // }

                await Task.WhenAll(tasks);
                //Util.Log("Wait more time");
                //Task.Delay(TimeSpan.FromSeconds(0)).Wait();

                /* end method 1 */


                ///* connection method 2:*/
                //var total = _pkg.Connections.Count;
                //var inner = 100;
                //var outer = total / inner;
                //var taskMatrix = new List<List<Task>>();
                //for (var i = 0; i < outer; i++)
                //{
                //    var sw = new Stopwatch();
                //    sw.Start();
                //    taskMatrix.Add(new List<Task>());
                //    for (var j = 0; j < inner; j++)
                //    {
                //        var ind = i * inner + j;
                //        taskMatrix[i].Add(_pkg.Connections[ind].StartAsync());
                //    }
                //    Task.WhenAll(taskMatrix[i]).Wait();
                //    sw.Stop();
                //    Util.Log($"finish epoach: {i}, elapsed time: {sw.Elapsed.TotalSeconds}");
                //}

                //Util.Log("Wait more time");
                //Task.Delay(TimeSpan.FromSeconds(30)).Wait();
                ///* end method 2 */


                /* connection method 3:*/
                /* end method 3 */






                stopWatch.Stop();
                Util.Log($"Successfully connect with {_pkg.Connections.Count} connetions, connection elapsed time: {stopWatch.Elapsed}");

                _pkg.Job.State = ClientState.Running;
            }
            catch (Exception ex)
            {
                Util.Log($"exception in starting connection: {ex}");
            }
        }
    }
}

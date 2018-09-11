using Bench.Common;
using Bench.RpcSlave.Worker.Counters;
using Bench.RpcSlave.Worker.Savers;
using Bench.RpcSlave.Worker.Serverless;
using Bench.RpcSlave.Worker.StartTimeOffsetGenerator;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using System.Net;

namespace Bench.RpcSlave.Worker.Operations
{
    abstract class RestSendMsgOp : BaseSignalrOp, IOperation
    {
        protected IStartTimeOffsetGenerator StartTimeOffsetGenerator;
        protected List<int> _sentMessages;
        protected WorkerToolkit _tk;
        protected HttpClient _client;
        protected string _serverName;
        protected ServiceUtils _serviceUtils;
        protected string _endpoint;
        protected string _content;

        public async Task Do(WorkerToolkit tk)
        {
            _tk = tk;
            var waitTime = 5 * 1000;
            Console.WriteLine($"wait time: {waitTime / 1000}s");
            _tk.State = Stat.Types.State.SendReady;
            Setup();
            await Task.Delay(5000);
            _tk.State = Stat.Types.State.SendRunning;
            await StartSendMsg();

            _tk.State = Stat.Types.State.SendComplete;
            Util.Log($"Sending Complete");
        }

        public override void SetCallbacks()
        {
            Util.Log($"scenario: {_tk.BenchmarkCellConfig.Scenario}");
            for (int i = _tk.ConnectionRange.Begin; i < _tk.ConnectionRange.End; i++)
            {
                var ind = i;

                _tk.ConnectionCallbacks.Add(_tk.Connections[i - _tk.ConnectionRange.Begin].On(ServiceUtils.MethodName,
                    (string server, string timestamp, string content) =>
                    {
                        var receiveTimestamp = Util.Timestamp();
                        var sendTimestamp = Convert.ToInt64(timestamp);
                        _tk.Counters.CountLatency(sendTimestamp, receiveTimestamp);
                        _tk.Counters.IncreaseReceivedMessageSize((ulong)content.Length);
                    }));
            }
        }

        public override void Setup()
        {
            _client = new HttpClient();
            _serverName = ServiceUtils.GenerateServerName();
            _serviceUtils = new ServiceUtils(_tk.ConnectionString);
            _endpoint = _serviceUtils.Endpoint;
            StartTimeOffsetGenerator = new RandomGenerator(new LocalFileSaver());
            if (!_tk.Init.ContainsKey(_tk.BenchmarkCellConfig.Step))
            {
                SetCallbacks();
                _tk.Init[_tk.BenchmarkCellConfig.Step] = true;
            }
        }

        public override async Task StartSendMsg()
        {
            var messageBlob = new byte[_tk.BenchmarkCellConfig.MessageSize];
            Random rnd = new Random();
            rnd.NextBytes(messageBlob);
            _content = UTF8Encoding.ASCII.GetString(messageBlob);
            var beg = _tk.ConnectionRange.Begin;
            var end = _tk.ConnectionRange.End;

            var sendCnt = 0;
            for (var i = beg; i < end; i++)
            {
                var cfg = _tk.ConnectionConfigList.Configs[i];
                if (cfg.SendFlag) sendCnt++;
            }
            if (_tk.Connections.Count == 0 || sendCnt == 0)
            {
                Util.Log($"nothing to do, wait scenario finish");
                await Task.Delay(TimeSpan.FromSeconds(_tk.JobConfig.Duration + 5));
            }
            else
            {
                Util.Log($"send count: {sendCnt}");
                ServicePointManager.DefaultConnectionLimit = sendCnt;
                var tasks = new List<Task>();
                for (var i = beg; i < end; i++)
                {
                    var cfg = _tk.ConnectionConfigList.Configs[i];
                    if (cfg.SendFlag)
                    {
                        tasks.Add(StartSendingMessageAsync(i, messageBlob,
                            _tk.JobConfig.Duration, _tk.JobConfig.Interval, _tk.Counters));
                    }   
                }
                await Task.WhenAll(tasks);
            }
        }

        protected async Task StartSendingMessageAsync(int index, byte[] messageBlob, int duration, int interval, Counter counter)
        {
            var messageSize = (ulong)messageBlob.Length;
            await Task.Delay(StartTimeOffsetGenerator.Delay(TimeSpan.FromSeconds(interval)));
            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(duration)))
            {
                while (!cts.IsCancellationRequested)
                {
                    try
                    {
                        var url = GenRestUrl(_serviceUtils, $"{index}");
                        var request = new HttpRequestMessage(HttpMethod.Post, GetUrl(url));

                        request.Headers.Authorization =
                            new AuthenticationHeaderValue("Bearer",
                                _serviceUtils.GenerateAccessToken(url, _serverName));
                        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                        var payloadRequest = new PayloadMessage
                        {
                            Target = ServiceUtils.MethodName,
                            Arguments = new[]
                            {
                                _serverName,
                                $"{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
                                _content
                            }
                        };
                        request.Content = new StringContent(JsonConvert.SerializeObject(payloadRequest), Encoding.UTF8, "application/json");
                        var response = await _client.SendAsync(request);
                        response.EnsureSuccessStatusCode();
                        counter.IncreaseSentMessageSize(messageSize);
                        counter.IncreseSentMsg();
                    }
                    catch (Exception ex)
                    {
                        Util.Log($"exception in sending message of {index}th connection: {ex}");
                        //counter.IncreaseConnectionError();
                        counter.IncreseNotSentFromClientMsg();
                    }
                }
            }
        }

        private Uri GetUrl(string baseUrl)
        {
            return new UriBuilder(baseUrl).Uri;
        }

        private class PayloadMessage
        {
            public string Target { get; set; }

            public object[] Arguments { get; set; }
        }

        // Generate the REST URL according to connection string (ServiceUtils),
        // HubName (ServiceUtils) and target (user/group/broadcast)
        public abstract string GenRestUrl(ServiceUtils serviceUtils, string arg);
    }
}

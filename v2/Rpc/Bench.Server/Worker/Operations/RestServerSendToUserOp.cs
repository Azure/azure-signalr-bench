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

namespace Bench.RpcSlave.Worker.Operations
{
    class RestServerSendToUserOp : BaseSignalrOp, IOperation
    {
        protected IStartTimeOffsetGenerator StartTimeOffsetGenerator;
        protected List<int> _sentMessages;
        protected WorkerToolkit _tk;
        protected HttpClient _client;
        protected string _serverName;
        protected ServiceUtils _serviceUtils;
        protected string _endpoint;

        public async Task Do(WorkerToolkit tk)
        {
            _tk = tk;

            Setup();

            _tk.State = Stat.Types.State.SendReady;

            await StartSendMsg();

            _tk.State = Stat.Types.State.SendComplete;
            Util.Log($"Sending Complete");
        }

        public override void SetCallbacks()
        {
            // nothing to do
        }

        public override void Setup()
        {
            _client = new HttpClient();
            _serverName = ServiceUtils.GenerateServerName();
            _serviceUtils = new ServiceUtils(_tk.ConnectionString);
            _endpoint = _serviceUtils.Endpoint;
            StartTimeOffsetGenerator = new RandomGenerator(new LocalFileSaver());
        }

        public override async Task StartSendMsg()
        {
            var messageBlob = new byte[_tk.BenchmarkCellConfig.MessageSize];
            Random rnd = new Random();
            rnd.NextBytes(messageBlob);

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
                var tasks = new List<Task>();
                for (var i = beg; i < end; i++)
                {
                    var cfg = _tk.ConnectionConfigList.Configs[i];
                    if (cfg.SendFlag)
                    {
                        await StartSendingMessageAsync(i, messageBlob,
                            _tk.JobConfig.Duration, _tk.JobConfig.Interval, _tk.Counters);
                    }   
                }
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
                        var url = _serviceUtils.GetSendToUserUrl(ServiceUtils.HubName,
                            $"{ServiceUtils.ClientUserIdPrefix}{index}");
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
                            }
                        };
                        request.Content = new StringContent(JsonConvert.SerializeObject(payloadRequest), Encoding.UTF8, "application/json");
                        var response = await _client.SendAsync(request);
                        response.EnsureSuccessStatusCode();
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
    }
}

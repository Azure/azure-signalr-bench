using Common;
using Plugin.Microsoft.Azure.SignalR.Benchmark.AgentMethods.Statistics;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark.AgentMethods
{
    public class BaseContinuousSendMethod
    {
        protected string Type;
        protected long Duration;
        protected long Interval;
        protected int MessageSize;
        protected int RemainderBegin;
        protected int RemainderEnd;
        protected int Modulo;

        protected StatisticsCollector StatisticsCollector;
        protected List<int> ConnectionIndex;
        protected IList<IHubConnectionAdapter> Connections;
        protected IDictionary<string, object> StepParameters;
        protected IDictionary<string, object> PluginParameters;
        protected bool HideRecordLatency;

        protected virtual void LoadParametersAndContext(
            IDictionary<string, object> stepParameters,
            IDictionary<string, object> pluginParameters)
        {
            // Get parameters
            stepParameters.TryGetTypedValue(SignalRConstants.Type, out Type, Convert.ToString);
            stepParameters.TryGetTypedValue(SignalRConstants.RemainderBegin, out RemainderBegin, Convert.ToInt32);
            stepParameters.TryGetTypedValue(SignalRConstants.RemainderEnd, out RemainderEnd, Convert.ToInt32);
            stepParameters.TryGetTypedValue(SignalRConstants.Modulo, out Modulo, Convert.ToInt32);
            stepParameters.TryGetTypedValue(SignalRConstants.Duration, out Duration, Convert.ToInt64);
            stepParameters.TryGetTypedValue(SignalRConstants.Interval, out Interval, Convert.ToInt64);
            stepParameters.TryGetTypedValue(SignalRConstants.MessageSize, out MessageSize, Convert.ToInt32);

            pluginParameters.TryGetTypedValue($"{SignalRConstants.StatisticsStore}.{Type}",
                out StatisticsCollector, obj => (StatisticsCollector)obj);
            pluginParameters.TryGetTypedValue($"{SignalRConstants.ConnectionStore}.{Type}",
                out Connections, (obj) => (IList<IHubConnectionAdapter>)obj);
            pluginParameters.TryGetTypedValue($"{SignalRConstants.ConnectionIndex}.{Type}",
                out ConnectionIndex, (obj) => (List<int>)obj);
            StepParameters = stepParameters;
            PluginParameters = pluginParameters;
        }

        protected void UpdateEpoch(StatisticsCollector statisticsCollector)
        {
            // Update epoch at the end of 'Wait' to ensure all the messages are received and all clients stop sending
            statisticsCollector.IncreaseEpoch();
        }

        protected void SetSendingStep(
            StatisticsCollector statisticsCollector,
            int sendingStep)
        {
            statisticsCollector.SetSendingStep(sendingStep);
        }

        protected void UpdateStatistics(
            StatisticsCollector statisticsCollector,
            int sendingStep)
        {
            SignalRUtils.ResetCounters(statisticsCollector);
            UpdateEpoch(statisticsCollector);
            SetSendingStep(statisticsCollector, sendingStep);
        }

        protected async Task ContinuousSend<T>(
            T connection,
            BenchMessage data,
            Func<T, BenchMessage, Task> f,
            TimeSpan duration,
            TimeSpan interval,
            TimeSpan delayMin,
            TimeSpan delayMax)
        {
            // Random delay in [delayMin, delayMax) ms
            var rand = new Random();
            var randomDelay = TimeSpan.FromMilliseconds(rand.Next((int)delayMin.TotalMilliseconds, (int)delayMax.TotalMilliseconds));
            await Task.Delay(randomDelay);

            // Send message continuously
            using var cts = new CancellationTokenSource(duration);
            while (!cts.IsCancellationRequested)
            {
                await f(connection, data); // TODO: await may cause bad send rate
                await Task.Delay(interval);
            }
        }

        protected async Task BaseSendAsync(
            (IHubConnectionAdapter Connection, int LocalIndex, string CallbackMethod) package,
            BenchMessage data)
        {
            try
            {
                // Is the connection is not active, then stop sending message
                if (package.Connection.GetStat() != SignalREnums.ConnectionInternalStat.Active)
                    return;
                var payload = GenPayload(data);
                using (var c = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
                {
                    await package.Connection.SendAsync(package.CallbackMethod, payload, c.Token);
                }
                // Update statistics
                SignalRUtils.RecordSend(payload, StatisticsCollector);
            }
            catch (Exception ex)
            {
                var message = $"Error in {GetType().Name}: {ex}";
                Log.Error(message);
            }
        }

        protected virtual async Task<IDictionary<string, object>> SimpleScenarioSend(
            IDictionary<string, object> stepParameters,
            IDictionary<string, object> pluginParameters,
            string callbackMethod)
        {
            try
            {
                Log.Information($"Start {GetType().Name}...");

                LoadParametersAndContext(stepParameters, pluginParameters);

                // Generate necessary data
                var data = new BenchMessage { MessageBlob = SignalRUtils.GenerateRandomData(MessageSize) };

                // Reset counters
                UpdateStatistics(StatisticsCollector, RemainderEnd);

                // Send messages
                await Task.WhenAll(
                    from i in Enumerable.Range(0, Connections.Count)
                    where ConnectionIndex[i] % Modulo >= RemainderBegin && ConnectionIndex[i] % Modulo < RemainderEnd
                    select ContinuousSend(
                        (Connection: Connections[i], LocalIndex: i, CallbackMethod: callbackMethod),
                        data,
                        BaseSendAsync,
                        TimeSpan.FromMilliseconds(Duration),
                        TimeSpan.FromMilliseconds(Interval),
                        TimeSpan.FromMilliseconds(1),
                        TimeSpan.FromMilliseconds(Interval)));
                Log.Information($"Finish {GetType().Name} {RemainderEnd}");
                return null;
            }
            catch (Exception ex)
            {
                var message = $"Fail to {GetType().Name}: {ex}";
                Log.Error(message);
                throw;
            }
        }

        protected BenchMessage GenPayload(BenchMessage data)
        {
            // Prepare payload
            var payload = data.Clone();
            payload.Timestamp = Util.Timestamp();
            return payload;
        }
    }
}

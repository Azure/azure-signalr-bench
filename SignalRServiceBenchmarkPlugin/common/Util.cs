using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Common
{
    public static class TimedoutTask
    {
        public static async Task TimeoutAfter(this Task task, TimeSpan timeout)
        {
            using (var timeoutCancellationTokenSource = new CancellationTokenSource())
            {
                var completedTask = await Task.WhenAny(task, Task.Delay(timeout, timeoutCancellationTokenSource.Token));
                if (completedTask == task)
                {
                    timeoutCancellationTokenSource.Cancel();
                    await task;  // Very important in order to propagate exceptions
                }
                else
                {
                    throw new TimeoutException("The operation has timed out.");
                }
            }
        }

        public static async Task<TResult> TimeoutAfter<TResult>(this Task<TResult> task, TimeSpan timeout)
        {
            using (var timeoutCancellationTokenSource = new CancellationTokenSource())
            {
                var completedTask = await Task.WhenAny(task, Task.Delay(timeout, timeoutCancellationTokenSource.Token));
                if (completedTask == task)
                {
                    timeoutCancellationTokenSource.Cancel();
                    return await task;  // Very important in order to propagate exceptions
                }
                else
                {
                    throw new TimeoutException("The operation has timed out.");
                }
            }
        }
    }

    public class Util
    {
        public static void CreateLogger(string directory, string name, LogTargetEnum logTarget)
        {
            switch(logTarget)
            {
                
                case LogTargetEnum.File:
                    Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Debug()
                    .WriteTo.File(Path.Combine(directory, name), rollingInterval: RollingInterval.Day)
                    .CreateLogger();
                    break;
                case LogTargetEnum.Console:
                    Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Debug()
                    .WriteTo.Console()
                    .CreateLogger();
                    break;
                default:
                case LogTargetEnum.All:
                    Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Debug()
                    .WriteTo.Console()
                    .WriteTo.File(Path.Combine(directory, name), rollingInterval: RollingInterval.Day)
                    .CreateLogger();
                    break;

            }
        }

        public static string ReadFile(string path)
        {
            try
            {
                var content = File.ReadAllText(path);
                return content;
            }
            catch (Exception ex)
            {
                Log.Error($"Fail to load benchmark configuration: {ex}");
                throw ex;
            }
        }

        public static void WriteFile(string content, string path, bool append=false)
        {
            using (StreamWriter sw = new StreamWriter(path, append))
            {
                sw.Write(content);
            }
        }

        public static void SavePidToFile(string pidFile)
        {
            var pid = Process.GetCurrentProcess().Id;
            if (pidFile != null)
            {
                WriteFile(Convert.ToString(pid), pidFile, false);
            }
        }

        public static int SplitNumber(int total, int index, int agents)
        {
            int baseNumber = total / agents;
            if (index < total % agents)
            {
                baseNumber++;
            }
            return baseNumber;
        }

        public static (int, int) GetConnectionRange(int total, int index, int agents)
        {
            var begin = 0;
            for (var i = 0; i < index; i++)
            {
                begin += SplitNumber(total, i, agents);
            }

            var end = begin + SplitNumber(total, index, agents);

            return (begin, end);
        }

        public static long Timestamp()
        {
            var unixDateTime = (long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalMilliseconds;
            return unixDateTime;
        }

        public static string Timestamp2DateTimeStr(long timestamp)
        {
            return DateTimeOffset.FromUnixTimeMilliseconds(timestamp).ToString("yyyy-MM-ddThh:mm:ssZ");
        }

        public static async Task LowPressBatchProcess<T>(IList<T> source, Func<T, Task> f, int max, int milliseconds)
        {
            var nextBatch = max;
            var left = source.Count;
            if (nextBatch <= left)
            {
                var tasks = new List<Task>(left);
                var i = 0;
                do
                {
                    for (var j = 0; j < nextBatch; j++)
                    {
                        var index = i + j;
                        var item = source[index];
                        tasks.Add(Task.Run(async () =>
                        {
                            await f(item);
                        }));
                    }

                    await Task.Delay(TimeSpan.FromMilliseconds(milliseconds));
                    i += nextBatch;
                    left = left - nextBatch;
                    if (left < nextBatch)
                    {
                        nextBatch = left;
                    }
                } while (left > 0);
                await Task.WhenAll(tasks);
            }
        }

        public static Task ExternalRateLimitBatchProcess<T>(
            IList<T> source,
            Func<T, Task> f,
            int capacity,
            int tokenFillPerInterval,
            int intervalMilliSeconds)
        {
            var tokenBucket = Esendex.TokenBucket.TokenBuckets
                .Construct()
                .WithCapacity(capacity)
                .WithFixedIntervalRefillStrategy(tokenFillPerInterval, TimeSpan.FromMilliseconds(intervalMilliSeconds))
                .WithBusyWaitSleepStrategy().Build();
            return Task.WhenAll(from item in source
                                select Task.Run(async () =>
                                {
                                    tokenBucket.Consume(1);
                                    try
                                    {
                                        await f(item);
                                    }
                                    catch (System.OperationCanceledException e)
                                    {
                                        Log.Warning($"see cancellation in {f.Method.Name}: {e.Message}");
                                    }
                                }));
        }

        public static async Task RateLimitBatchProces<T>(
            IList<T> source,
            Func<T, Task> f,
            int capacity,
            int tokenFillPerInterval,
            int intervalMilliSeconds)
        {
            using (var tokenBucket =
                SemaphoreTokenBucketBuilder.Builder()
                .WithCapacity(capacity)
                .WithRefill(tokenFillPerInterval, TimeSpan.FromMilliseconds(intervalMilliSeconds))
                .Build())
            {
                await Task.WhenAll(from item in source
                                 select Task.Run(async () =>
                                 {
                                     try
                                     {
                                         await tokenBucket.WaitAsync();
                                         try
                                         {
                                             await f(item);
                                         }
                                         catch (System.OperationCanceledException e)
                                         {
                                             Log.Warning($"see cancellation in {f.Method.Name}: {e.Message}");
                                         }
                                         finally
                                         {
                                             tokenBucket.Release();
                                         }
                                     }
                                     catch (Exception e)
                                     {
                                         Log.Error($"{e.Message}");
                                     }
                                 }));
            }
        }

        // TODO:
        // Hardcode a time out value for cancellation token.
        // For some time consuming operations, this time out needs to be tuned.
        public static Task BatchProcess<T>(IList<T> source, Func<T, Task> f, int max)
        {
            var initial = (max >> 1);
            var s = new System.Threading.SemaphoreSlim(initial, max);
            _ = Task.Run(async () =>
            {
                for (int i = initial; i < max; i++)
                {
                    await Task.Delay(100);
                    s.Release();
                }
            });

            return Task.WhenAll(from item in source
                                select Task.Run(async () =>
                                {
                                    var retry = 0;
                                    var maxRetry = 5;
                                    var rand = new Random();
                                    while (retry < maxRetry)
                                    {
                                        try
                                        {
                                            await s.WaitAsync();
                                            try
                                            {
                                                await f(item);
                                                break;
                                            }
                                            catch (System.OperationCanceledException e)
                                            {
                                                Log.Warning($"see cancellation in {f.Method.Name}: {e.Message}");
                                            }
                                            finally
                                            {
                                                s.Release();
                                            }
                                        }
                                        catch (System.OperationCanceledException)
                                        {
                                            Log.Warning($"Waiting too long time to obtain the semaphore: current: {s.CurrentCount}, max: {max}");
                                        }
                                        var randomDelay = TimeSpan.FromMilliseconds(rand.Next(1, 500));
                                        await Task.Delay(randomDelay);
                                        retry++;
                                    }
                                    if (retry == maxRetry)
                                    {
                                        Log.Error($"The operation {f.Method.Name} was canceled because of reaching max retry {maxRetry}");
                                    }
                                }));
        }

        public static Task<TOut[]> BatchProcess<T, TOut>(IList<T> source, Func<T, Task<TOut>> f, int max)
        {
            var initial = (max >> 1);
            var s = new System.Threading.SemaphoreSlim(initial, max);
            _ = Task.Run(async () =>
            {
                for (int i = initial; i < max; i++)
                {
                    await Task.Delay(100);
                    s.Release();
                }
            });

            return Task.WhenAll(from item in source
                                select Task.Run(async () =>
                                {
                                    TOut ret = default;
                                    var retry = 0;
                                    var maxRetry = 5;
                                    while (retry < maxRetry)
                                    {
                                        try
                                        {
                                            using (var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(60)))
                                            {
                                                await s.WaitAsync(cancellationTokenSource.Token);
                                                try
                                                {
                                                    ret = await f(item);
                                                    break;
                                                }
                                                catch (System.OperationCanceledException e)
                                                {
                                                    Log.Warning($"see cancellation in {f.Method.Name}: {e.Message}");
                                                }
                                                finally
                                                {
                                                    s.Release();
                                                }
                                            }
                                        }
                                        catch (System.OperationCanceledException)
                                        {
                                            Log.Warning($"Waiting too long time to obtain the semaphore: current: {s.CurrentCount}, max: {max}");
                                        }
                                        var rand = new Random();
                                        var randomDelay = TimeSpan.FromMilliseconds(rand.Next(1, 500));
                                        await Task.Delay(randomDelay);
                                        retry++;
                                    }
                                    if (retry == maxRetry)
                                    {
                                        Log.Error($"The operation {f.Method.Name} was canceled because of reaching max retry {maxRetry}");
                                    }
                                    return ret;
                                }));
        }

        public static Task TimeoutCheckedTask(Task task, long millisecondsToWait, string taskName = "timeout checked task")
        {
            try
            {
                var finalTask = TimedoutTask.TimeoutAfter(task, TimeSpan.FromMilliseconds(millisecondsToWait));
                Log.Information($"Finish {taskName}");
                return finalTask;
            }
            catch (TimeoutException)
            {
                Log.Error($"The '{taskName}' timedout after {millisecondsToWait} ms");
            }
            return Task.CompletedTask;
        }

        public static string GenerateServerName()
        {
            return $"{Environment.MachineName}_{Guid.NewGuid():N}";
        }
    }
}

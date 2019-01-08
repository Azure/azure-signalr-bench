using Serilog;
using System;
using System.Collections.Generic;
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
                                    using (var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(60)))
                                    {
                                        await s.WaitAsync(cancellationTokenSource.Token);
                                        try
                                        {
                                            await f(item);
                                        }
                                        finally
                                        {
                                            s.Release();
                                        }
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
                                    using (var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(60)))
                                    {
                                        await s.WaitAsync(cancellationTokenSource.Token);
                                        try
                                        {
                                            var res = await f(item);
                                            return res;
                                        }
                                        finally
                                        {
                                            s.Release();
                                        }
                                    }
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
    }
}

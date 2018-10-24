using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Common
{
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
                                    await s.WaitAsync();
                                    try
                                    {
                                        await f(item);
                                    }
                                    finally
                                    {
                                        s.Release();
                                    }
                                }));
        }
    }
}

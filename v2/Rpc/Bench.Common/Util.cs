using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Newtonsoft.Json.Linq;

namespace Bench.Common
{
    public static class ThreadSafeRandom
    {
        [ThreadStatic] private static Random Local;

        public static Random ThisThreadsRandom
        {
            get { return Local ?? (Local = new Random(unchecked(Environment.TickCount * 31 + Thread.CurrentThread.ManagedThreadId))); }
        }
    }

    public static class MyExtensions
    {
        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = ThreadSafeRandom.ThisThreadsRandom.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        // Circle left shift of values on index of 
        // 0, 1, 2, ..., n-2, n-1 to 1, 2, 3, ..., n-1, 0
        public static void CircleLeftShift<T>(this IList<T> list)
        {
            var n = list.Count;
            if (n > 1)
            {
                T v = list[0];
                for (var i = 1; i < n; i++)
                {
                    list[i - 1] = list[i];
                }
                list[n - 1] = v;
            }
        }
    }

    public class Util
    {
        public static void Log(string message)
        {
            var time = DateTime.Now.ToString("hh:mm:ss.fff");
            ColorWriteLine($"[{time}] {message}");
        }

        public static void ColorWriteLine(string text, ConsoleColor color = ConsoleColor.DarkYellow)
        {
            ConsoleColor originalColor = Console.BackgroundColor;
            Console.BackgroundColor = color;
            Console.WriteLine(text);
            Console.BackgroundColor = originalColor;
        }

        public static long Timestamp()
        {
            var unixDateTime = (long) (DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalMilliseconds;
            return unixDateTime;
        }

        public static JObject Sort(JObject jobj)
        {
            //Util.Log($"jobj = {jobj.ToString()}");
            var sorted = new JObject(
                jobj.Properties().OrderBy(p =>
                {
                    //Util.Log($"p.name = {p.Name}");
                    var startInd = p.Name.LastIndexOf(":") + 1;
                    int latency = 99999;
                    try
                    {
                        latency = Convert.ToInt32((p.Name.Substring(startInd)));
                    }
                    catch (Exception) { }

                    if (p.Name.Contains("ge"))
                    {
                        latency += 1;
                    }

                    if (p.Name.Contains("receive"))
                    {
                        latency -= 1;
                    }

                    if (p.Name.Contains("connection"))
                    {
                        latency += 10;
                        if (p.Name.Contains("error"))
                        {
                            latency += 1;
                        }
                    }

                    return latency;
                })
            );
            //Util.Log($"sorted jobj = {sorted.ToString()}");
            return sorted;
        }

        public static string Timestamp2DateTimeStr(long timestamp)
        {
            return DateTimeOffset.FromUnixTimeMilliseconds(timestamp).ToString("yyyy-MM-ddThh:mm:ssZ");
        }

        public static class GuidEncoder
        {
            public static string Encode(string guidText)
            {
                Guid guid = new Guid(guidText);
                return Encode(guid);
            }

            public static string Encode(Guid guid)
            {
                string enc = Convert.ToBase64String(guid.ToByteArray());
                enc = enc.Replace("/", "_");
                enc = enc.Replace("+", "-");
                return enc.Substring(0, 22);
            }

            public static Guid Decode(string encoded)
            {
                encoded = encoded.Replace("_", "/");
                encoded = encoded.Replace("-", "+");
                byte[] buffer = Convert.FromBase64String(encoded + "==");
                return new Guid(buffer);
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

        public static void SaveContentToFile(string path, string content, bool append)
        {
            var resDir = System.IO.Path.GetDirectoryName(path);
            if (!String.IsNullOrEmpty(resDir) && !Directory.Exists(resDir))
            {
                Directory.CreateDirectory(resDir);
            }
            using(StreamWriter sr = new StreamWriter(path, append))
            {
                sr.Write(content);
            }
        }

        public static void LogList<T>(string title, List<T> list)
        {
            Util.Log(title);
            list.ForEach(el => Util.Log(el.ToString()));
        }

        public static byte[] GenerateMessageBlob(int sizeInByte)
        {
            var messageBlob = new byte[sizeInByte];
            return messageBlob;
        }

        public static bool isDebug(string debug)
        {
            return debug == "debug";
        }

        public static string TrimPrefix(string input)
        {
            var trimmed = Regex.Replace(input, @"^[A-Za-z]+", "");
            return trimmed;
        }

    }
}
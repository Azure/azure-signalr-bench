using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;

namespace JenkinsScript
{
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
            var unixDateTime = (long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalMilliseconds;
            return unixDateTime;
        }

        public static JObject Sort(JObject jobj)
        {
            Util.Log($"jobj = {jobj.ToString()}");
            var sorted = new JObject(
                jobj.Properties().OrderBy(p =>
                {
                    Util.Log($"p.name = {p.Name}");
                    var startInd = p.Name.LastIndexOf(":") + 1;
                    int latency = 99999;
                    try
                    {
                        latency = Convert.ToInt32((p.Name.Substring(startInd)));
                    }
                    catch (Exception)
                    {
                    }

                    if (p.Name.IndexOf("ge") >= 0)
                    {
                        latency += 1;
                    }

                    Util.Log($"latencay = {latency}");
                    return latency;
                })
            );
            Util.Log($"sorted jobj = {sorted.ToString()}");
            return sorted;
        }

        public static string GenRandPrefix()
        {
            var rnd = new Random();
            var srRndNum = (rnd.Next(10000) * rnd.Next(10000)).ToString();
            return srRndNum;
        }

        public static string GenResourceGroupName(string postfix)
        {
            return "group" + postfix;
        }

        public static string GenSignalRServiceName(string postfix)
        {
            var rnd = new Random();
            var SrRndNum = (rnd.Next(10000) * rnd.Next(10000)).ToString();

            // SignalR Service name will be used in http url, so upper case will be automatically changed to lower case.
            // In order to avoid confusion, please use lower case in SignalR service naming.
            return "sr" + postfix;
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
    }
}

using System;

namespace Client.UtilNs
{
    public class Util
    {
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

        public static void Log(string message)
        {
            var time = DateTime.Now.ToString("hh:mm:ss.fff");
            ColorWriteLine($"[{time}] {message}");
        }
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

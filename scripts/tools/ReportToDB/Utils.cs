using System;

namespace ReportToDB
{
    public class Utils
    {
        public static DateTime ConvertFromTimestamp(string timestamp)
        {
            var year = Convert.ToInt16(timestamp.Substring(0, 4));
            var month = Convert.ToInt16(timestamp.Substring(4, 2));
            var day = Convert.ToInt16(timestamp.Substring(6, 2));
            var hour = Convert.ToInt16(timestamp.Substring(8, 2));
            var minute = Convert.ToInt16(timestamp.Substring(10, 2));
            var second = Convert.ToInt16(timestamp.Substring(12, 2));
            var dt = new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc);
            return dt;
        }
    }
}

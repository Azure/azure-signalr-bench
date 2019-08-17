using System;

namespace ReportToDB
{
    public static class ReportRecordExtensions
    {
        public static int Unit(this ReportRecord reportRecord)
        {
            var scenario = reportRecord.Scenario;
            var dashIndex = scenario.IndexOf('_');
            if (dashIndex != -1)
            {
                var s = scenario.Substring(0, dashIndex);
                var unitEndIndex = s.LastIndexOf('t'); // find "unit" from last index
                if (unitEndIndex != -1 && unitEndIndex + 1 < s.Length)
                {
                    var unit = s.Substring(unitEndIndex + 1, s.Length - unitEndIndex - 1);
                    return Convert.ToInt32(unit);
                }
            }
            return 0;
        }
    }
}

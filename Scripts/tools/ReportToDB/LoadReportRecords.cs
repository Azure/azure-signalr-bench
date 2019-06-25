using System;
using System.Collections.Generic;
using System.IO;

namespace ReportToDB
{
    public class LoadReportRecords
    {
        public static IEnumerable<ReportRecord> GetReportRecords(string fileName)
        {
            string line;
            var fileHandle = new StreamReader(fileName);
            try
            {
                while ((line = fileHandle.ReadLine()) != null)
                {
                    if (!string.IsNullOrEmpty(line))
                    {
                        var statistics = BuildReportRecord(line);
                        yield return statistics;
                    }
                }
            }
            finally
            {
                fileHandle.Close();
            }
        }

        private static ReportRecord BuildReportRecord(string strContent)
        {
            var items = strContent.Split(',');
            var reportRecord = new ReportRecord()
            {
                Timestamp = items[0],
                Scenario = items[1],
                Connections = Convert.ToInt32(items[2]),
                Sends = Convert.ToInt32(items[3]),
                SendTPuts = Convert.ToInt64(items[4]),
                RecvTPuts = Convert.ToInt64(items[5]),
                Reference = items[6]
            };

            return reportRecord;
        }
    }
}

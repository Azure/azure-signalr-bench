using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

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
            ReportRecord reportRecord = null;
            var items = strContent.Split(',');
            reportRecord = new ReportRecord()
            {
                Timestamp = items[0],
                Scenario = items[1],
                Connections = Convert.ToInt32(items[2]),
                Sends = Convert.ToInt32(items[3]),
                SendTPuts = Convert.ToInt64(items[4]),
                RecvTPuts = Convert.ToInt64(items[5]),
                Reference = items[6]
            };
            if (items.Length >= 11)
            {
                reportRecord.DroppedConnections = Convert.ToInt32(items[7]);
                reportRecord.ReconnCost99Percent = Convert.ToInt32(items[8]);
                reportRecord.LifeSpan99Percent = Convert.ToInt32(items[9]);
                reportRecord.Offline99Percent = Convert.ToInt32(items[10]);
                if (items.Length > 11)
                {
                    var othersBuilder = new StringBuilder();
                    for (var i = 11; i < items.Length; i++)
                    {
                        if (i == 11)
                        {
                            othersBuilder.Append(items[i]);
                        }
                        else
                        {
                            othersBuilder.Append("|").Append(items[i]);
                        }
                    }
                    reportRecord.Others = othersBuilder.ToString();
                }
                reportRecord.HasConnectionStat = true;
            }
            return reportRecord;
        }
    }
}

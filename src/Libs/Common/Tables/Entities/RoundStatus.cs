using System;
using System.Collections.Generic;
using Azure.SignalRBench.Common;

namespace Azure.SignalRBench.Coordinator.Entities
{
    public class RoundStatus : ReportClientStatusParameters
    {
        private const double Tolerance = 0.05;
        private const double MinimumConnectedRatio = 0.75;

        public int ActiveConnection { get; set; }
        public int RoundConnected { get; set; }
        public RoundCheckFailure[] CheckFailures { get; set; } = Array.Empty<RoundCheckFailure>();

        public RoundCheckFailure[] GetCheckFailures()
        {
            var failures = new List<RoundCheckFailure>();
            if (ReconnectingCount > RoundConnected * Tolerance)
            {
                failures.Add(new RoundCheckFailure("Reconnecting connections",
                    FormattableString.Invariant($"<= {100 * Tolerance:0.##}%"),
                    Percentage(ReconnectingCount, RoundConnected)));
            }

            if (MessageRecieved == 0)
            {
                failures.Add(new RoundCheckFailure("Messages received", "> 0 messages",
                    FormattableString.Invariant($"0 (expected {ExpectedRecievedMessageCount})")));
            }
            else if (MessageRecieved < ExpectedRecievedMessageCount * (1 - Tolerance))
            {
                failures.Add(new RoundCheckFailure("Messages received",
                    FormattableString.Invariant($">= {100 * (1 - Tolerance):0.##}% of expected"),
                    Percentage(MessageRecieved, ExpectedRecievedMessageCount)));
            }

            if (ConnectedCount < RoundConnected * MinimumConnectedRatio)
            {
                failures.Add(new RoundCheckFailure("Connected connections",
                    FormattableString.Invariant($">= {100 * MinimumConnectedRatio:0.##}% of target"),
                    Percentage(ConnectedCount, RoundConnected)));
            }

            if (MessageRecieved > 0)
            {
                var slowMessages = Latency[LatencyClass.LessThan2s] + Latency[LatencyClass.LessThan5s] +
                                   Latency[LatencyClass.MoreThan5s];
                if (slowMessages > MessageRecieved * Tolerance)
                {
                    failures.Add(new RoundCheckFailure("Messages with latency >= 1s",
                        FormattableString.Invariant($"<= {100 * Tolerance:0.##}% of received"),
                        Percentage(slowMessages, MessageRecieved)));
                }
            }

            return failures.ToArray();
        }

        private static string Percentage(int count, int total) =>
            FormattableString.Invariant($"{100.0 * count / total:0.00}% ({count}/{total})");
    }

    public class RoundCheckFailure
    {
        public RoundCheckFailure(string metric, string required, string actual)
        {
            Metric = metric;
            Required = required;
            Actual = actual;
        }

        public string Metric { get; }
        public string Required { get; }
        public string Actual { get; }
    }
}
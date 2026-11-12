using System;
using System.Linq;
using Azure.SignalRBench.Common;
using Azure.SignalRBench.Coordinator.Entities;
using Newtonsoft.Json;
using Xunit;

namespace Azure.SignalRBench.Tests
{
    public class RoundStatusTest
    {
        [Fact]
        public void ReportsEveryFailedCheckWithActualValuesAndThresholds()
        {
            var round = CreateRound();
            round.ReconnectingCount = 6;
            round.MessageRecieved = 90;
            round.ConnectedCount = 74;
            round.Latency[LatencyClass.LessThan2s] = 6;

            var failures = round.GetCheckFailures();

            Assert.Equal(new[]
            {
                ("Reconnecting connections", "<= 5%", "6.00% (6/100)"),
                ("Messages received", ">= 95% of expected", "90.00% (90/100)"),
                ("Connected connections", ">= 75% of target", "74.00% (74/100)"),
                ("Messages with latency >= 1s", "<= 5% of received", "6.67% (6/90)")
            }, failures.Select(failure => (failure.Metric, failure.Required, failure.Actual)));
        }

        [Fact]
        public void ThresholdEqualityPassesAndCumulativeReconnectsAreNotChecked()
        {
            var round = CreateRound();
            round.ReconnectingCount = 5;
            round.TotalReconnectCount = 1000;
            round.ConnectedCount = 75;
            round.ExpectedRecievedMessageCount = 2000;
            round.MessageRecieved = 1900;
            round.Latency[LatencyClass.LessThan2s] = 95;

            Assert.Empty(round.GetCheckFailures());
        }

        [Theory]
        [InlineData(LatencyClass.LessThan1s, false)]
        [InlineData(LatencyClass.LessThan2s, true)]
        [InlineData(LatencyClass.LessThan5s, true)]
        [InlineData(LatencyClass.MoreThan5s, true)]
        public void LatencyCheckCountsOnlyBucketsStartingAtOneSecond(LatencyClass bucket, bool fails)
        {
            var round = CreateRound();
            round.Latency[bucket] = 6;

            var failures = round.GetCheckFailures();

            Assert.Equal(fails ? 1 : 0, failures.Length);
            if (fails)
            {
                Assert.Equal("Messages with latency >= 1s", failures[0].Metric);
                Assert.Equal("<= 5% of received", failures[0].Required);
                Assert.Equal("6.00% (6/100)", failures[0].Actual);
            }
        }

        [Fact]
        public void NoMessagesExplainsTheFailureWithoutDividingByZero()
        {
            var round = CreateRound();
            round.MessageRecieved = 0;
            round.ExpectedRecievedMessageCount = 0;
            round.Latency.Clear();

            var failure = Assert.Single(round.GetCheckFailures());
            Assert.Equal("Messages received", failure.Metric);
            Assert.Equal("> 0 messages", failure.Required);
            Assert.Equal("0 (expected 0)", failure.Actual);
        }

        [Fact]
        public void ReportKeepsTheRecordedReasonsInsteadOfRecomputingThem()
        {
            var round = CreateRound();
            round.ConnectedCount = 74;
            round.CheckFailures = round.GetCheckFailures();
            round.ConnectedCount = 100;

            var saved = JsonConvert.DeserializeObject<RoundStatus>(JsonConvert.SerializeObject(round));

            Assert.Empty(saved.GetCheckFailures());
            var failure = Assert.Single(saved.CheckFailures);
            Assert.Equal("Connected connections", failure.Metric);
            Assert.Equal(">= 75% of target", failure.Required);
            Assert.Equal("74.00% (74/100)", failure.Actual);
        }

        private static RoundStatus CreateRound() => new RoundStatus
        {
            RoundConnected = 100,
            ConnectedCount = 100,
            MessageRecieved = 100,
            ExpectedRecievedMessageCount = 100,
            Latency = Enum.GetValues(typeof(LatencyClass)).Cast<LatencyClass>()
                .ToDictionary(bucket => bucket, _ => 0)
        };
    }
}

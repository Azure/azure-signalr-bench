using System;
using System.IO;
using Azure.SignalRBench.Common;
using Azure.SignalRBench.Coordinator.Entities;
using Xunit;

namespace Azure.SignalRBench.Tests
{
    public class TestFailureSummaryTest
    {
        [Theory]
        [InlineData("Response status code does not indicate success: 429 (Too Many Requests).",
            "The endpoint throttled connection requests (HTTP 429).")]
        [InlineData("The server disconnected before sending a handshake response",
            "The server closed the connection before the SignalR handshake completed.")]
        [InlineData("An existing connection was forcibly closed by the remote host.",
            "The remote connection was reset during setup.")]
        [InlineData("Authentication failed because the remote party has closed the transport stream.",
            "The remote endpoint closed the connection during TLS setup.")]
        [InlineData("WebSockets failed: A task was canceled.",
            "Connection attempts timed out or were cancelled before completing.")]
        [InlineData("Unable to connect to the remote server",
            "A connection to the remote endpoint could not be established.")]
        [InlineData("An unfamiliar transport error.", "An unfamiliar transport error.")]
        public void ExplainsConnectionFailuresWithoutGuessingTheUnderlyingRootCause(string error, string reason)
        {
            var exception = new InvalidOperationException(
                "Test sample--1, phase round 4: connecting, command StartClientConnections failed: " +
                "Command StartClientConnections, AckId 93, sender sample--1-13 failed. " +
                "387 of 1000 connections failed to connect after 3 attempts. First error: " + error);

            Assert.Equal("Connection target not reached: 387/1000 new connections failed after 3 attempts.\n" +
                reason + "\nWhere: round 4: connecting; pod sample--1-13.", TestFailureSummary.Describe(exception));
        }

        [Fact]
        public void ExplainsMissingCommandResponses()
        {
            var error = new TimeoutException(
                "Test sample--1, phase round 2: scenario, command StopScenario failed: " +
                "Command StopScenario, AckId 12 timed out; missing senders: sample--1-0, sample--1-1.");

            Assert.Equal("Timed out waiting for test pods to respond to StopScenario.\n" +
                "Missing pods: sample--1-0, sample--1-1.\nWhere: round 2: scenario.",
                TestFailureSummary.Describe(error));
        }

        [Fact]
        public void DescribesParticipantReplacementWithoutCallingItAnOom()
        {
            var error = new InvalidDataException(
                "Test sample--1, phase round 2: scenario: participant sample--1-server " +
                "changed instance/role: AppServers/old -> AppServers/new.");

            Assert.Equal("A test pod reported a different process identity or role during the run.\n" +
                "Pod: sample--1-server; phase: round 2: scenario.", TestFailureSummary.Describe(error));
        }

        [Fact]
        public void ExplainsUnsupportedReadyMessagesInsideExceptionWrappers()
        {
            var error = new InvalidOperationException("Startup failed.", new InvalidDataException(
                "Test sample--1, phase Ready: unsupported Ready protocol or invalid sender/role " +
                "from 'sample--1-0'; InstanceId is required."));

            Assert.Equal("A pod sent an invalid readiness message.\n" +
                "Check that the participant and coordinator images use the same Ready protocol.",
                TestFailureSummary.Describe(error));
        }

        [Fact]
        public void UnknownErrorsKeepTheirOriginalExceptionAndAreNotRelabeledByCleanupErrors()
        {
            var original = new InvalidOperationException("An unfamiliar error with 429 in its message.");
            var status = new TestStatusEntity();
            status.RecordError(original);
            status.RecordError(new InvalidDataException(
                "Test sample--1, phase Ready: unsupported Ready protocol or invalid sender/role."));

            Assert.Null(status.ErrorSummary);
            Assert.Contains(original.ToString(), status.ErrorInfo);
        }

        [Fact]
        public void CleanupDoesNotOverwriteThePrimaryFailureSummary()
        {
            var status = new TestStatusEntity();
            status.RecordError(new TimeoutException(
                "Command StartScenario, AckId 3 timed out; missing senders: client-0."));
            var summary = status.ErrorSummary;

            status.RecordError(new IOException("Failed to delete a pod."));

            Assert.Equal(summary, status.ErrorSummary);
            Assert.Contains("Failed to delete a pod.", status.ErrorInfo);
        }
    }
}

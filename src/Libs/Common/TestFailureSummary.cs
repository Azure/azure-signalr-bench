using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Azure.SignalRBench.Common
{
    public static class TestFailureSummary
    {
        public static string? Describe(Exception error)
        {
            for (Exception? current = error; current != null; current = current.InnerException)
            {
                var summary = DescribeMessage(current.Message);
                if (summary != null)
                    return summary;
            }
            return null;
        }

        private static string? DescribeMessage(string message)
        {
            // Remote exceptions arrive as text; match only the harness's known message formats.
            var connections = Regex.Match(message,
                @"(?<failed>\d+) of (?<total>\d+) connections failed to connect after (?<attempts>\d+) attempts\. First error: (?<error>.+)",
                RegexOptions.Singleline);
            if (connections.Success)
            {
                return $"Connection target not reached: {connections.Groups["failed"].Value}/{connections.Groups["total"].Value} " +
                       $"new connections failed after {connections.Groups["attempts"].Value} attempts.\n" +
                       ConnectionReason(connections.Groups["error"].Value) + CommandContext(message);
            }

            var timeout = Regex.Match(message,
                @"Command (?<command>\w+), AckId \d+ timed out; missing senders: (?<pods>[^\r\n]+)\.");
            if (timeout.Success)
            {
                return $"Timed out waiting for test pods to respond to {timeout.Groups["command"].Value}.\n" +
                       $"Missing pods: {timeout.Groups["pods"].Value}." + CommandContext(message);
            }

            var replacement = Regex.Match(message,
                @"phase (?<phase>.+?): participant (?<pod>\S+) changed instance/role:");
            if (replacement.Success)
            {
                return "A test pod reported a different process identity or role during the run.\n" +
                       $"Pod: {replacement.Groups["pod"].Value}; phase: {replacement.Groups["phase"].Value}.";
            }

            if (message.Contains("unsupported Ready protocol or invalid sender/role"))
            {
                return "A pod sent an invalid readiness message.\n" +
                       "Check that the participant and coordinator images use the same Ready protocol.";
            }

            return null;
        }

        private static string ConnectionReason(string error)
        {
            if (error.Contains("429") && error.Contains("Too Many Requests"))
                return "The endpoint throttled connection requests (HTTP 429).";
            if (error.Contains("server disconnected before sending a handshake response"))
                return "The server closed the connection before the SignalR handshake completed.";
            if (Contains(error, "connection reset") || Contains(error, "connection was reset") ||
                Contains(error, "forcibly closed"))
                return "The remote connection was reset during setup.";
            if (Contains(error, "remote party has closed the transport stream"))
                return "The remote endpoint closed the connection during TLS setup.";
            if (Contains(error, "timed out") || Contains(error, "task was canceled") ||
                Contains(error, "operation was canceled"))
                return "Connection attempts timed out or were cancelled before completing.";
            if (Contains(error, "Unable to connect to the remote server"))
                return "A connection to the remote endpoint could not be established.";
            return error;
        }

        private static string CommandContext(string message)
        {
            var context = new List<string>();
            var phase = Regex.Match(message, @"phase (?<phase>[^,]+), command \S+ failed:");
            if (phase.Success)
                context.Add(phase.Groups["phase"].Value);
            var sender = Regex.Match(message, @"sender (?<pod>\S+) failed\.");
            if (sender.Success)
                context.Add("pod " + sender.Groups["pod"].Value);
            return context.Count == 0 ? string.Empty : "\nWhere: " + string.Join("; ", context) + ".";
        }

        private static bool Contains(string text, string value) =>
            text.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}

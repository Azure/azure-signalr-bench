// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Azure.SignalRBench.Common
{
    public static class Commands
    {
        public static class General
        {
            public static readonly string Crash = "Crash";
        }

        public static class Clients
        {
            public static readonly string StartClientConnections = "StartClientConnections";
            public static readonly string CloseClientConnections = "CloseClientConnections";
            public static readonly string JoinGroups = "JoinGroups";
            public static readonly string StartScenario = "StartScenario";
            public static readonly string StopScenario = "StopScenario";
            public static readonly string SetScenario = "SetScenario";
        }

        public static class AppServer
        {
            public static readonly string GracefulShutdownThenRestart = "GracefulShutdownThenRestart";
        }

        public static class Coordinator
        {
            public static readonly string ReportReady = "ReportReady";
            public static readonly string ReportClientStatus = "ReportClientStatus";
        }
    }
}

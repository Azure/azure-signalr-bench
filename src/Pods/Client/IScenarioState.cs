// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;

using Azure.SignalRBench.Common;

namespace Azure.SignalRBench.Client
{
    public interface IScenarioState
    {
        public void SetClientRange(SetClientRangeParameters setClientRangeParameters);

        public Task StartClientConnections(StartClientConnectionsParameters startClientConnectionsParameters);

        public void SetSenario(SetScenarioParameters setScenarioParameters);

        public void StartSenario(StartScenarioParameters startScenarioParameters);

        public void StopSenario(StopScenarioParameters stopScenario);

        public Task StopClientConnections(StopClientConnectionsParameters StopClientConnectionsParameters);

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Bench.Common;
using Bench.RpcSlave.Worker.Rest;
using Bench.RpcSlave.Worker.Serverless;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Bench.RpcSlave.Worker.Operations
{
    class CreateRestClientConnOp : BaseOp, IOperation
    {
        private WorkerToolkit _tk;

        public Task Do(WorkerToolkit tk)
        {
            _tk = tk;
            // here url is connectionString, we borrow it for not changing RPC model
            Util.Log($"server url: {_tk.JobConfig.ServerUrl}; conn: {_tk.JobConfig.Connections};  _tk.BenchmarkCellConfig.TransportType: { _tk.BenchmarkCellConfig.TransportType}; _tk.BenchmarkCellConfig.HubProtocol: {_tk.BenchmarkCellConfig.HubProtocol}");
            _tk.State = Stat.Types.State.HubconnUnconnected;
            _tk.ConnectionString = _tk.JobConfig.ServerUrl;
            _tk.Connections = Create(_tk.ConnectionRange.Begin, _tk.ConnectionRange.End,
                _tk.ConnectionString, _tk.BenchmarkCellConfig.TransportType,
                _tk.BenchmarkCellConfig.HubProtocol);

            if (tk.Connections == null) Util.Log("connections == null");
            Util.Log($"xxxconnections: {_tk.Connections.Count}");
            return Task.CompletedTask;
        }

        private List<HubConnection> Create(int startCliIndex, int endCliIndex, string connectionString,
            string transportTypeName = "Websockets",
            string hubProtocol = "json")
        {
            Util.Log($"transport type: {transportTypeName}");

            _tk.State = Stat.Types.State.HubconnCreating;
            var connections = new List<HubConnection>(endCliIndex - startCliIndex);
            for (var i = startCliIndex; i < endCliIndex; i++)
            {
                var connection = ConnectionUtils.CreateSingleDirectConnection(_tk, connectionString, i);
                connections.Add(connection);
            }

            _tk.State = Stat.Types.State.HubconnCreated;
            return connections;
        }
    }
}

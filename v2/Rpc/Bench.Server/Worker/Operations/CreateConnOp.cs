using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Bench.Common;
using Bench.RpcSlave.Worker.Rest;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Bench.RpcSlave.Worker.Operations
{
    class CreateConnOp : BaseOp, IOperation
    {
        private WorkerToolkit _tk;
        public Task Do(WorkerToolkit tk)
        {
            _tk = tk;

            Util.Log($"server url: {_tk.JobConfig.ServerUrl}; conn: {_tk.JobConfig.Connections};  _tk.BenchmarkCellConfig.TransportType: { _tk.BenchmarkCellConfig.TransportType}; _tk.BenchmarkCellConfig.HubProtocol: {_tk.BenchmarkCellConfig.HubProtocol}");
            _tk.State = Stat.Types.State.HubconnUnconnected;
            var count = _tk.ConnectionRange.End - _tk.ConnectionRange.Begin;
            ConnectionUtils.CreateBrokenConnectionTrackList(_tk, count);
            _tk.Connections = Create(count, _tk.JobConfig.ServerUrl, _tk.BenchmarkCellConfig.TransportType, _tk.BenchmarkCellConfig.HubProtocol);

            if (tk.Connections == null) Util.Log("connections == null");
            Util.Log($"xxxconnections: {_tk.Connections.Count}");
            return Task.CompletedTask;
        }

        private List<HubConnection> Create(int conn, string url,
            string transportTypeName = "Websockets",
            string hubProtocol = "json")
        {
            //_tk.State = Stat.Types.State.HubconnCreating;
            Util.Log($"transport type: {transportTypeName}");
            var connections = new List<HubConnection>(conn);
            for (var i = 0; i < conn; i++)
            {
                var connection = ConnectionUtils.CreateSingleConnection(_tk, i);
                connections.Add(connection);
            }
            _tk.State = Stat.Types.State.HubconnCreated;
            return connections;
        }
    }
}
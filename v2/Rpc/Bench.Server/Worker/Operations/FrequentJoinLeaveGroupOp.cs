using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bench.Common;
using Microsoft.AspNetCore.SignalR.Client;

/*

Op

	One
		create new connection
		start new connection
		join all groups


	loop
		Other join some groups
		One send a message to all groups
		Other leave groups


	stop One's connection
	disppose One's connection

 */

namespace Bench.RpcSlave.Worker.Operations
{
    class FrequentJoinLeaveGroupOp : JoinLeaveGroupOp, IOperation
    {
        public async Task Do(WorkerToolkit tk)
        {
            
        }
        
        private async Task<List<HubConnection>> prepareOne(string serverUrl, string transportType, string hubProtocol, List<string> groupNameList)
        {
            // create and start connections
            var tkOne = new WorkerToolkit();
            tkOne.JobConfig = new Common.Config.JobConfig();
            tkOne.ConnectionRange.Begin = 0;
            tkOne.ConnectionRange.End = 1;
            tkOne.JobConfig.ServerUrl = serverUrl;
            tkOne.BenchmarkCellConfig.TransportType = transportType;
            tkOne.BenchmarkCellConfig.HubProtocol = hubProtocol;
            var createConnOp = new CreateConnOp();
            await createConnOp.Do(tkOne);
            foreach (var connection in tkOne.Connections)
            {
                await connection.StartAsync();
            }

            // join all groups
            foreach (var connection in tkOne.Connections)
            {
                foreach (var groupName in groupNameList)
                {
                    await connection.SendAsync("JoinGroup", groupName, "perf");

                }
            }

            return tkOne.Connections;
        }
    }

}
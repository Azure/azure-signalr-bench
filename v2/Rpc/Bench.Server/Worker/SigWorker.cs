using Bench.Common.Config;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Text;
using Bench.RpcSlave.Worker.Operations;
using Bench.Common;

namespace Bench.RpcSlave.Worker
{
    class SigWorker
    {
        private WorkerToolkit _tk = new WorkerToolkit();

        public void LoadJobs(JobConfig jobConfig)
        {
            _tk.JobConfig = jobConfig;
        }

        public void LoadBenchmarkCellConfig(BenchmarkCellConfig benchmarkCellConfig)
        {
            _tk.BenchmarkCellConfig = benchmarkCellConfig;
        }

        public void LoadConnectionConfig(ConnectionConfigList connectionConfigList)
        {
            _tk.ConnectionConfigList = connectionConfigList;
        }

        public void LoadConnectionRange(Range range)
        {
            _tk.ConnectionRange  = range;
        }

        public Stat.Types.State ProcessJob(string opName)
        {
            // process operations
            //GetPipeline().ForEach(opName =>
            //{
            var tuple = OperationFactory.CreateOp(opName, _tk);
            var obj = tuple.Item1;
            var type = tuple.Item2;
            dynamic op = Convert.ChangeType(obj, type);
            op.Do(_tk);
            //});

            return _tk.State;
        }

        public List<string> GetPipeline()
        {
            Console.WriteLine($"pipeline: {_tk.JobConfig.Pipeline}");
            return _tk.JobConfig.Pipeline;
        }

        public List<Tuple<string, int>> GetCounters()
        {
            return _tk.Counters.GetAll();
        }

        public void UpdateState(Stat.Types.State state)
        {
            _tk.State = state;
        }

        public Stat.Types.State GetState()
        {
            return _tk.State;
        }
    }
}

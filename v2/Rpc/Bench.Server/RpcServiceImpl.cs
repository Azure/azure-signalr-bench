using Bench.Common;
using Bench.Common.Config;
using Bench.RpcSlave.Worker;
using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Bench.RpcSlave.Worker.Operations;

namespace Bench.RpcSlave
{
    public class RpcServiceImpl : RpcService.RpcServiceBase
    {
        SigWorker _sigWorker;

        public override Task<Timestamp> GetTimestamp(Empty empty, ServerCallContext context)
        {
            return Task.FromResult(new Timestamp { Time = 123 });
        }

        public override Task<Stat> GetState(Empty empty, ServerCallContext context)
        {
            try
            {
                return Task.FromResult(new Stat { State = _sigWorker.GetState() });
            }
            catch (Exception ex)
            {
                Util.Log($"Exception: {ex}");
                throw;
            }
        }

        public override Task<Strg> GetCounterJsonStr(Empty empty, ServerCallContext context)
        {
            return Task.FromResult(new Strg { Str = "json string" });
        }

        public override Task<Stat> LoadJobConfig(CellJobConfig config, ServerCallContext context)
        {
            try
            {
                var jobConfig = new JobConfig
                {
                    Connections = config.Connections,
                    ConcurrentConnections = config.ConcurrentConnections,
                    Slaves = config.Slaves,
                    Interval = config.Interval,
                    Duration = config.Duration,
                    ServerUrl = config.ServerUrl,
                    Pipeline = new List<string>(config.Pipeline.Split(';'))
                };

                // TODO: handle exception
                if (_sigWorker == null)
                {
                    _sigWorker = new SigWorker();
                }

                _sigWorker.LoadJobs(jobConfig);
                _sigWorker.UpdateState(Stat.Types.State.ConfigLoaded);
                return Task.FromResult(new Stat { State = Stat.Types.State.ConfigLoaded });
            }
            catch (Exception ex)
            {
                Util.Log($"Exception: {ex}");
                throw;
            }
        }

        public override Task<Stat> CreateWorker(Empty empty, ServerCallContext context)
        {
            try
            {
                if (_sigWorker != null)
                {
                    _sigWorker.UpdateState(Stat.Types.State.WorkerExisted);
                    return Task.FromResult(new Stat { State = Stat.Types.State.WorkerExisted });
                }

                _sigWorker = new SigWorker();
                _sigWorker.UpdateState(Stat.Types.State.WorkerCreated);
                return Task.FromResult(new Stat { State = Stat.Types.State.WorkerCreated });
            }
            catch (Exception ex)
            {
                Util.Log($"Exception: {ex}");
                throw;
            }
        }

        public override Task<Dict> CollectCounters(Force force, ServerCallContext context)
        {
            try
            {
                var dict = new Dict();
                if (force.Force_ != true && (int)_sigWorker.GetState() < (int)Stat.Types.State.SendRunning)
                {
                    return Task.FromResult(dict);
                }

                var list = _sigWorker.GetCounters();
                list.ForEach(pair => dict.Pairs.Add(new Pair { Key = pair.Item1, Value = pair.Item2 }));
                return Task.FromResult(dict);
            }
            catch (Exception ex)
            {
                Util.Log($"Exception: {ex}");
                throw;
            }
        }

        public override Task<Stat> RunJob(Common.BenchmarkCellConfig cellConfig, ServerCallContext context)
        {
            try
            {
                Console.WriteLine($"Run Job");
                // Worker.BenchmarkCellConfig benchmarkCellConfig = new Worker.BenchmarkCellConfig
                // {
                //     ServiceType = cellConfig.ServiveType,
                //     HubProtocol = cellConfig.HubProtocol,
                //     TransportType = cellConfig.TransportType,
                //     Scenario = cellConfig.Scenario
                    
                // };
                Console.WriteLine($"LoadBenchmarkCellConfig");
                _sigWorker.LoadBenchmarkCellConfig(cellConfig);

                Console.WriteLine($"ProcessJob step: {cellConfig.Step}");
                _sigWorker.ProcessJob(cellConfig.Step);

                return Task.FromResult(new Stat { State = Stat.Types.State.DebugTodo });
            }
            catch (Exception ex)
            {
                Util.Log($"Exception: {ex}");
                throw;
            }
        }

        public override Task<Empty> LoadConnectionConfig(ConnectionConfigList connectionConfigList, ServerCallContext context)
        {
            _sigWorker.LoadConnectionConfig(connectionConfigList);
            return Task.FromResult(new Empty());
        }

        public override Task<Empty> LoadConnectionRange(Range connectionRange, ServerCallContext context)
        {
            Util.Log($"connection ind range for current client: {connectionRange.Begin} {connectionRange.End}");
            _sigWorker.LoadConnectionRange(connectionRange);
            return Task.FromResult(new Empty());
        }

        public override Task<Stat> Test(Strg strg, ServerCallContext context)
        {
            return Task.FromResult(new Stat { State = Stat.Types.State.DebugTodo});
        }
    }
}

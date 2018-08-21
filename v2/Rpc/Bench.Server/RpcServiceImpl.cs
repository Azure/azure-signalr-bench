using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Bench.Common;
using Bench.Common.Config;
using Bench.RpcSlave.Worker;
using Bench.RpcSlave.Worker.Operations;
using Grpc.Core;

namespace Bench.RpcSlave
{
    public class RpcServiceImpl : RpcService.RpcServiceBase
    {
        SigWorker _sigWorker;

        public override Task<Timestamp> GetTimestamp(Empty empty, ServerCallContext context)
        {
            return Task.FromResult(new Timestamp { Time = 123 });
        }

        public override Task<Timestamp> GetStateStr(Timestamp send, ServerCallContext context)
        {
            var curTime = Util.Timestamp();
            Util.Log($"receive from master time: {(ulong)curTime - send.Time} ms");
            return Task.FromResult(new Timestamp { Time = (ulong) curTime });
        }

        public override Task<Stat> GetState(Empty empty, ServerCallContext context)
        {
            try
            {
                var state = Task.FromResult(new Stat { State = _sigWorker.GetState() });
                return state;
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
                        // Slaves = config.Slaves,
                        Interval = config.Interval,
                        Duration = config.Duration,
                        ServerUrl = config.ServerUrl,
                        Pipeline = new List<string>(config.Pipeline.Split(';')),
                        OneSend = config.OneSend
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
                if (force.Force_ != true && (int) _sigWorker.GetState() < (int) Stat.Types.State.SendRunning)
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

        public override async Task<Stat> RunJob(Common.BenchmarkCellConfig cellConfig, ServerCallContext context)
        {
            try
            {
                Console.WriteLine($"Run Job");

                Console.WriteLine($"LoadBenchmarkCellConfig");
                _sigWorker.LoadBenchmarkCellConfig(cellConfig);

                Console.WriteLine($"ProcessJob step: {cellConfig.Step}");
                await _sigWorker.ProcessJob(cellConfig.Step);

                return new Stat { State = Stat.Types.State.DebugTodo };
                // return Task.FromResult(new Stat { State = Stat.Types.State.DebugTodo });
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
            return Task.FromResult(new Stat { State = Stat.Types.State.DebugTodo });
        }

        public override Task<StrgList> GetConnectionIds(Empty empty, ServerCallContext context)
        {
            return Task.FromResult(_sigWorker.GetConnectionIds());
        }
    }
}
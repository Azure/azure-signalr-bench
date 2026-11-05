// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Azure.SignalRBench.Common;
using Newtonsoft.Json.Linq;

namespace Azure.SignalRBench.Messages
{
    public static class MessageClientExtensions
    {
        private static readonly string ProcessInstanceId = Guid.NewGuid().ToString("N");

        #region General

        public static async Task<CommandMessage> CrashAsync(this IMessageClient client, string instanceId)
        {
            var message = new CommandMessage { Command = Commands.General.Crash };
            await client.SendCommandAsync(instanceId, message);
            return message;
        }

        public static Task<CommandMessage> CrashClientsAsync(this IMessageClient client) =>
            CrashAsync(client, Roles.Clients);

        public static Task<CommandMessage> CrashAppServersAsync(this IMessageClient client) =>
            CrashAsync(client, Roles.AppServers);

        #endregion

        #region Commands To Clients
        
        public static async Task<CommandMessage> BroadcastCoordinateTime(this IMessageClient client)
        {
            var command = new SetCoordinatorTimeParameters()
            {
                CoordinatorTime = DateTime.UtcNow.Ticks,
            };
            var message = new CommandMessage { Command = Commands.Clients.SetCoordinatorTime, Parameters = JObject.FromObject(command) };
            await client.SendCommandAsync(Roles.Clients, message);
            return message;
        }
        
        public static async Task<CommandMessage> StartClientConnectionsAsync(this IMessageClient client, StartClientConnectionsParameters parameters)
        {
            var message = new CommandMessage { Command = Commands.Clients.StartClientConnections, Parameters = JObject.FromObject(parameters) };
            await client.SendCommandAsync(Roles.Clients, message);
            return message;
        }

        public static async Task<CommandMessage> StopClientConnectionsAsync(this IMessageClient client, StopClientConnectionsParameters parameters)
        {
            var message = new CommandMessage { Command = Commands.Clients.StopClientConnections, Parameters = JObject.FromObject(parameters) };
            await client.SendCommandAsync(Roles.Clients, message);
            return message;
        }

        public static async Task<CommandMessage> SetScenarioAsync(this IMessageClient client, SetScenarioParameters parameters)
        {
            var message = new CommandMessage { Command = Commands.Clients.SetScenario, Parameters = JObject.FromObject(parameters) };
            await client.SendCommandAsync(Roles.Clients, message);
            return message;
        }

        public static async Task<CommandMessage> StartScenarioAsync(this IMessageClient client, StartScenarioParameters parameters)
        {
            var message = new CommandMessage { Command = Commands.Clients.StartScenario, Parameters = JObject.FromObject(parameters) };
            await client.SendCommandAsync(Roles.Clients, message);
            return message;
        }

        public static async Task<CommandMessage> StopScenarioAsync(this IMessageClient client, StopScenarioParameters parameters)
        {
            var message = new CommandMessage { Command = Commands.Clients.StopScenario, Parameters = JObject.FromObject(parameters) };
            await client.SendCommandAsync(Roles.Clients, message);
            return message;
        }

        public static async Task<CommandMessage> SetClientRangeAsync(this IMessageClient client, string clientInstance, SetClientRangeParameters parameters)
        {
            var message = new CommandMessage { Command = Commands.Clients.SetClientRange, Parameters = JObject.FromObject(parameters) };
            await client.SendCommandAsync(clientInstance, message);
            return message;
        }
        
        public static async Task<CommandMessage> StopTestAsync(this IMessageClient client)
        {
            var message = new CommandMessage { Command = Commands.AppServer.GracefulShutdown };
            await client.SendCommandAsync(Roles.AppServers, message);
            return message;
        }

        #endregion

        #region Commands To AppServer

        public static async Task<CommandMessage> AppServerGracefulShutdownThenRestartAsync(this IMessageClient client, string serverId)
        {
            var message = new CommandMessage { Command = Commands.AppServer.GracefulShutdown };
            await client.SendCommandAsync(serverId, message);
            return message;
        }

        #endregion

        #region Commands To Coordinator

        public static async Task<CommandMessage> ReportReadyAsync(this IMessageClient client, ReportReadyParameters parameters)
        {
            parameters.InstanceId = ProcessInstanceId;
            var message = new CommandMessage { Command = Commands.Coordinator.ReportReady, Parameters = JObject.FromObject(parameters) };
            await client.SendCommandAsync(Roles.Coordinator, message);
            return message;
        }

        public static async Task<CommandMessage> ReportClientStatusAsync(this IMessageClient client, ReportClientStatusParameters parameters)
        {
            var message = new CommandMessage { Command = Commands.Coordinator.ReportClientStatus, Parameters = JObject.FromObject(parameters) };
            await client.SendCommandAsync(Roles.Coordinator, message);
            return message;
        }

        #endregion

        #region Ack

        public static Task AckCompletedAsync(this IMessageClient client, CommandMessage command) =>
            client.AckAsync(command, AckStatus.Completed);

        public static Task AckCanceledAsync(this IMessageClient client, CommandMessage command) =>
            client.AckAsync(command, AckStatus.Canceled);

        public static Task AckFaultedAsync(this IMessageClient client, CommandMessage command, string error) =>
            client.AckAsync(command, AckStatus.Faulted, error);

        public static Task AckProgressAsync(this IMessageClient client, CommandMessage command, double progress) =>
            client.AckAsync(command, AckStatus.Running, progress: progress);

        public static async Task SendCommandAndWaitForAcksAsync(
            this IMessageClient messageClient,
            string target,
            CommandMessage request,
            IReadOnlyCollection<string> expectedSenders,
            CancellationToken cancellationToken,
            TimeSpan? timeout = null)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var deadline = timeout ?? TimeSpan.FromMinutes(60);
            if (deadline <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(timeout));
            messageClient.PrepareCommand(request);
            var pending = new HashSet<string>(expectedSenders, StringComparer.Ordinal);
            var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var gate = new object();
            var terminal = false;
            var publicationCompleted = false;
            using (var cancellation = cancellationToken.Register(() => completion.TrySetCanceled(cancellationToken)))
            using (var timer = new Timer(_ =>
            {
                lock (gate)
                {
                    if (!terminal)
                        completion.TrySetException(new TimeoutException(
                            $"Command {request.Command}, AckId {request.AckId} timed out; publication {(publicationCompleted ? "completed" : "pending")}; missing senders: {(pending.Count == 0 ? "none" : string.Join(", ", pending.OrderBy(sender => sender)))}."));
                }
            }, null, deadline, Timeout.InfiniteTimeSpan))
            {
                IDisposable? scope = null;
                try
                {
                    var registration = messageClient.WithHandlers(MessageHandler.CreateAckHandler(request.Command, ack =>
                    {
                        lock (gate)
                        {
                            if (terminal || completion.Task.IsCompleted || ack.AckId != request.AckId ||
                                !pending.Contains(ack.Sender))
                                return Task.CompletedTask;
                            switch (ack.Status)
                            {
                                case AckStatus.Canceled:
                                    if (cancellationToken.IsCancellationRequested)
                                        completion.TrySetCanceled(cancellationToken);
                                    else
                                        completion.TrySetException(new RpcFaultedException(
                                            $"Command {request.Command}, AckId {request.AckId}, sender {ack.Sender} canceled remotely. {ack.Error}"));
                                    break;
                                case AckStatus.Faulted:
                                    completion.TrySetException(new RpcFaultedException(
                                        $"Command {request.Command}, AckId {request.AckId}, sender {ack.Sender} failed. {ack.Error}"));
                                    break;
                                case AckStatus.Completed:
                                    pending.Remove(ack.Sender);
                                    if (pending.Count == 0 && publicationCompleted)
                                        completion.TrySetResult(true);
                                    break;
                            }
                        }
                        return Task.CompletedTask;
                    }));
                    if (await Task.WhenAny(registration, completion.Task) != registration)
                    {
                        // Subscription cannot be canceled; its eventual scope still belongs to this call.
                        ObserveFault(registration.ContinueWith(task =>
                        {
                            if (task.IsFaulted)
                                _ = task.Exception;
                            else if (!task.IsCanceled)
                                task.Result.Dispose();
                        }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default));
                        await completion.Task;
                    }
                    scope = await registration;
                    cancellationToken.ThrowIfCancellationRequested();
                    if (completion.Task.IsCompleted)
                        await completion.Task;
                    var publication = messageClient.PublishPreparedCommandAsync(target, request);
                    ObserveFault(publication);
                    var first = await Task.WhenAny(publication, completion.Task);
                    await first;
                    lock (gate)
                    {
                        publicationCompleted = true;
                        if (pending.Count == 0)
                            completion.TrySetResult(true);
                    }
                    await completion.Task;
                }
                finally
                {
                    lock (gate)
                        terminal = true;
                    scope?.Dispose();
                }
            }
        }

        private static void ObserveFault(Task task) =>
            _ = task.ContinueWith(faulted => { _ = faulted.Exception; },
                CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);

        #endregion
    }
}

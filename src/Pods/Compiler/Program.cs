// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Caching.Redis;

namespace Compiler
{
    class Program
    {
        public static IServer GetServer(ConnectionMultiplexer Connection, string host, int port)
        {
            return Connection.GetServer(host, port);
        }
        
        private static ConnectionMultiplexer CreateConnection()
        {
                string cacheConnection = "signalru1000redis7.redis.cache.windows.net:6380,password=BvmFODTa8uaClTeCn0v2xCUcEy2vFuxzVfiO6jkpzmc=,ssl=True,abortConnect=False,allowAdmin=true";
                return ConnectionMultiplexer.Connect(cacheConnection);
        }
        
        static async Task Main(string[] args)
        {
            
            Console.WriteLine("Hello World!");

            var connection = CreateConnection();
            var list = new List<ConnectionMultiplexer>();
            for (int i = 0; i < 100; i++)
            {
                var c = CreateConnection();
                var s = c.GetSubscriber();
                var cmq = await s.SubscribeAsync(
                    $"12345");
                list.Add(c);
            }
            var server = connection.GetServer("signalru1000redis7.redis.cache.windows.net",6380);
            ClientInfo[] clients = server.ClientList();

            Console.WriteLine("Cache response :");
            foreach (ClientInfo client in clients)
            {
                Console.WriteLine(client.Raw);
            }
            
            Console.WriteLine(clients.Length);
            await Task.Delay(10000000);

        }
        
        
    }
}

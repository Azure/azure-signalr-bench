// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Client.ClientJobNs;

namespace Client.WorkerNs
{
    static public class WorkerFactory
    {
        static public BaseWorker CreateWorker(ClientJob clientJob)
        {
            BaseWorker worker = new BaseWorker(clientJob);
            //switch (clientJob.Client)
            //{
            //    //case Worker.SignalRCoreEcho:
            //    //    worker = new SignalRCoreEchoWorker(clientJob);
            //    //    break;
            //    default:
            //        worker = new BaseWorker(clientJob);
            //}
            return worker;
        }
    }
}

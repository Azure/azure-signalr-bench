// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Azure.SignalRBench.Storage;

namespace Azure.SignalRBench.Coordinator
{
    public class PerfStorageProvider
    {
        private PerfStorage? _storage;
        public string? ConnectionString { get; private set; }

        public PerfStorage Storage => _storage ?? throw new InvalidOperationException();

        public void Initialize(string connectionString)
        {
            ConnectionString = connectionString;
            _storage = new PerfStorage(connectionString);
        }
    }
}
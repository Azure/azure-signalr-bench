﻿using System;
using System.Runtime.InteropServices.ComTypes;
using Azure.SignalRBench.Common;
using Microsoft.Azure.Cosmos.Table;

namespace Azure.SignalRBench.Coordinator.Entities
{
    public class TestStatusEntity : TableEntity
    {
        public string User { get; set; }

        public string Status { get; set; }

        public string Report { get; set; }

        public string Config { get; set; }

        public string? ErrorInfo { get; set; }

        public bool Healthy { get; set; }
        
        public string Dir { get; set; }
        
        public string? Check { get; set; }
        
        //Used for ev2 track only
        public string JobState { get; set; } = TestState.InProgress.ToString();
    }
}
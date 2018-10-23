﻿using CommandLine;
using Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Commander
{
    // TODO: edit descriptions and default values
    public class ArgsOption
    {
        // App server
        // App server
        [Option("AppserverPort", Required = false, Default = 5050, HelpText = "Port of app server.")]
        public int AppserverPort { get; set; }

        // Remote
        [Option("RpcPort", Required = false, Default = 5050, HelpText = "Port to be conencted from remote hosts.")]
        public int RpcPort { get; set; }

        [Option("SlaveList", Required = true, Separator = ',',  HelpText = "Target hosts to connect.")]
        public IList<string> SlaveList { get; set; }

        [Option("MasterHostname", Required = true, HelpText = "Hostname of master.")]
        public string MasterHostname { get; set; }

        [Option("AppServerHostname", Required = true, HelpText = "Hostname of app server.")]
        public string AppServerHostname { get; set; }

        [Option("Username", Required = true, HelpText = "Username of slaves.")]
        public string Username { get; set; }

        [Option("Password", Required = true, HelpText = "Password of slaves.")]
        public string Password { get; set; }

        // Log
        [Option("LogName", Required = false, Default = "master-.log", HelpText = "Log file name. Timestamp will insert into the position before dot. If the name is 'master-.log'. The final file name will be 'master-123456789.log' for example.")]
        public string LogName { get; set; }

        [Option("LogDirectory", Required = false, Default = ".", HelpText = "Directory in which the log will be stored.")]
        public string LogDirectory { get; set; }

        [Option("LogTarget", Required = false, Default = LogTargetEnum.All, HelpText = "Log target. " +
            "Options: All/File/Console." + " All: Output to file and console;" +
            " Console: Output to console;" + " File: Output to file.")]
        public LogTargetEnum LogTarget { get; set; }

        // Project 
        [Option("AppserverProject", Required = true, HelpText = "App server project.")]
        public string AppserverProject { get; set; }

        [Option("MasterProject", Required = true, HelpText = "Master project.")]
        public string MasterProject { get; set; }

        [Option("SlaveProject", Required = true, HelpText = "Slave project.")]
        public string SlaveProject { get; set; }

        [Option("AppserverTargetPath", Required = true, HelpText = "")]
        public string AppserverTargetPath { get; set; }

        [Option("MasterTargetPath", Required = true, HelpText = "")]
        public string MasterTargetPath { get; set; }

        [Option("SlaveTargetPath", Required = true, HelpText = "")]
        public string SlaveTargetPath { get; set; }

        [Option("BenchmarkConfiguration", Required = true, HelpText = "")]
        public string BenchmarkConfiguration { get; set; }

        [Option("BenchmarkConfigurationTargetPath", Required = true, HelpText = "")]
        public string BenchmarkConfigurationTargetPath { get; set; }


    }
}
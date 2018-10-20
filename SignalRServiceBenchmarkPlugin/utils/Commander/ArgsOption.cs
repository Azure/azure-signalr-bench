using CommandLine;
using Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Commander
{
    // TODO: edit descriptions and default values
    public class ArgsOption
    {
        // Remote
        [Option("SlaveList", Required = false, Separator = ',', Default = new string[] { "localhost:5050", "localhost:6060" }, HelpText = "Target hosts to connect.")]
        public IList<string> SlaveList { get; set; }

        [Option("MasterHostname", Required = true, Default = ".", HelpText = "Hostname of master.")]
        public string MasterHostname { get; set; }

        [Option("AppServerHostname", Required = true, Default = ".", HelpText = "Hostname of app server.")]
        public string AppServerHostname { get; set; }

        [Option("Username", Required = true, Default = ".", HelpText = "Username of slaves.")]
        public string Username { get; set; }

        [Option("Password", Required = true, Default = ".", HelpText = "Password of slaves.")]
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
        [Option("AppserverProject", Required = false, Default = ".", HelpText = "App server project.")]
        public string AppserverProject { get; set; }

        [Option("MasterProject", Required = false, Default = ".", HelpText = "Master project.")]
        public string MasterProject { get; set; }

        [Option("SlaveProject", Required = false, Default = ".", HelpText = "Slave project.")]
        public string SlaveProject { get; set; }

        [Option("AppserverTargetPath", Required = false, Default = ".", HelpText = "")]
        public string AppserverTargetPath { get; set; }

        [Option("MasterTargetPath", Required = false, Default = ".", HelpText = "")]
        public string MasterTargetPath { get; set; }

        [Option("SlaveTargetPath", Required = false, Default = ".", HelpText = "")]
        public string SlaveTargetPath { get; set; }
    }
}

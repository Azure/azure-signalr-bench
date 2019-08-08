using System.Collections.Generic;

namespace Rpc.Service
{
    public enum RpcLogTargetEnum
    {
        File,
        Console,
        All
    }

    public class RpcConfig
    {
        #region common configuration
        /// <summary>
        /// File name for pid
        /// </summary>
        public string PidFile { get; set; }

        /// <summary>
        /// Set the log target: file/console/all
        /// </summary>
        public RpcLogTargetEnum LogTarget { get; set; } = RpcLogTargetEnum.All;

        /// <summary>
        /// Set the log file name prefix
        /// </summary>
        public string LogName { get; set; }

        /// <summary>
        /// Set the log directory
        /// </summary>
        public string LogDirectory { get; set; } = ".";

        #endregion

        #region agent configuration
        /// <summary>
        /// Agent listening port
        /// </summary>
        public int RpcPort { get; set; } = 7000;

        /// <summary>
        /// Agent binding address
        /// </summary>
        public string HostName { get; set; } = "localhost";
        #endregion

        #region master configuration
        /// <summary>
        /// IP:Port list of agent nodes
        /// </summary>
        public IList<string> AgentList { get; set; }

        /// <summary>
        /// Set the plugin full name
        /// </summary>
        public string PluginFullName { get; set; }

        /// <summary>
        /// Set the plugin configuration file. If it is '?', the plugin will print help usage
        /// </summary>
        public string PluginConfiguration { get; set; }
        #endregion
    }
}

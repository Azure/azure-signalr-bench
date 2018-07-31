using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using CommandLine;

namespace Bench.Common.Config
{

    public class ConnectionConfigBuilder
    {

        public ConnectionConfigList Build(int totalConnection, int groupNum, int sendConCount)
        {
            var configs = new List<(string, bool)>();

            for (var i = 0; i < groupNum; i++)
            {
                var count = Util.SplitNumber(totalConnection, i, groupNum);
                configs.AddRange(Enumerable.Repeat(($"group_{i}", false), count).ToList());
            }

            for (var i = 0; i < totalConnection; i++)
            {
                var sendFlag = sendConCount-- > 0 ? true : false;
                configs[i] = (configs[i].Item1, sendFlag);
            }

            Util.Log($"connection config count: {configs.Count}");
            configs.Shuffle();
            var connectionConfigList = new ConnectionConfigList();

            // foreach (var groupName in configs)
            for (int i = 0; i < configs.Count; i++)
            {
                var groupName = configs[i].Item1;
                var sendFlag = configs[i].Item2;
                connectionConfigList.Configs.Add(new ConnectionConfig { GroupName = groupName, SendFlag = sendFlag });
            }
            return connectionConfigList;
        }

        public ConnectionConfigList Build(int totalConnection)
        {
            var configs = new List<bool>();

            for (var i = 0; i < totalConnection; i++)
            {
                var sendFlag = false;
                configs.Add(sendFlag);
            }

            configs.Shuffle();
            var connectionConfigList = new ConnectionConfigList();

            // foreach (var groupName in configs)
            for (int i = 0; i < configs.Count; i++)
            {
                var sendFlag = configs[i];
                connectionConfigList.Configs.Add(new ConnectionConfig { GroupName = "", SendFlag = sendFlag });
            }

            return connectionConfigList;
        }

        public ConnectionConfigList UpdateSendConn(ConnectionConfigList configs, int more)
        {
            var idleConnInds = new List<int>();
            for (var i = 0; i < configs.Configs.Count; i++)
            {
                if (!configs.Configs[i].SendFlag) idleConnInds.Add(i);
            }
            idleConnInds.Shuffle();
            for (var i = 0; i < more; i++)
            {
                configs.Configs[idleConnInds[i]].SendFlag = true;
            }

            return configs;
        }
    }
}
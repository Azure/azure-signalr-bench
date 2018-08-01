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

        public ConnectionConfigList UpdateSendConn(ConnectionConfigList configs, int more, int totalConnection, int slaveCnt)
        {
            // var curMores = new List<int>();
            // for (int i = 0; i < slaveCnt; i++)
            // {
            //     var curMore = Util.SplitNumber(more, i, slaveCnt);
            //     curMores.Add(curMore);
            // }
            // curMores.Shuffle();
            if (more % slaveCnt != 0 || totalConnection % slaveCnt != 0)
            {
                Util.Log($"more % slaveCnt != 0 || totalConnection % slaveCnt != 0");
                throw new Exception();
            }

            var beg = 0;
            for (var i = 0; i < slaveCnt; i++)
            {
                var curConnCnt = Util.SplitNumber(totalConnection, i, slaveCnt);
                var end = beg + curConnCnt;
                var curConnSlice = configs.Configs.ToList().GetRange(beg, end - beg);
                var idleConnInds = curConnSlice.Select((val, ind) => new { val, ind })
                    .Where(z => z.val.SendFlag == false)
                    .Select(z => z.ind).ToList();
                // Util.LogList($"idle inds", idleConnInds);
                idleConnInds.Shuffle();
                var curMore = Util.SplitNumber(more, i, slaveCnt);
                for (int j = 0; j < curMore && j < idleConnInds.Count; j++)
                {
                    Util.Log($"ind: {idleConnInds[j] + beg}");
                    configs.Configs[idleConnInds[j] + beg].SendFlag = true;
                }
                beg = end;
            }
            return configs;
        }
    }
}
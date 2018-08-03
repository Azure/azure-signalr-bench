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

        private int GetNewTotalSendingConnectionCount(List<ConnectionConfig> sendFlagList)
        {
            var curSendConn = 0;
            sendFlagList.ForEach(el => curSendConn += el.SendFlag ? 1 : 0);
            return curSendConn;
        }

        private List<int> GetCurrentSendingConnectionCountList(int connCnt, int slaveCnt)
        {
            var list = new List<int>();
            for (int i = 0; i < slaveCnt; i++)
            {
                list.Add(Util.SplitNumber(connCnt, i, slaveCnt));
            }
            return list;
        }

        private List<int> GetMoreSendingConnectionCountList(List<int> curSendConnList, List<int> newSendConnList)
        {
            var list = new List<int>();
            if (curSendConnList.Count != newSendConnList.Count) return list;
            for (int i = 0; i < curSendConnList.Count; i++)
            {
                list.Add(newSendConnList[i] - curSendConnList[i]);
            }
            return list;
        }
        public ConnectionConfigList UpdateSendConn(ConnectionConfigList configs, int more, int totalConnection, int slaveCnt)
        {
            var curTotalSendConn = GetNewTotalSendingConnectionCount(configs.Configs.ToList());
            var newTotalSendConn = curTotalSendConn + more;
            var curSendConnList = GetCurrentSendingConnectionCountList(curTotalSendConn, slaveCnt);
            var newSendConnList = GetCurrentSendingConnectionCountList(newTotalSendConn, slaveCnt);
            var moreSendConnList = GetMoreSendingConnectionCountList(curSendConnList, newSendConnList);

            var beg = 0;
            for (var i = 0; i < slaveCnt; i++)
            {
                var curConnCnt = Util.SplitNumber(totalConnection, i, slaveCnt);
                var end = beg + curConnCnt;
                var curConnSlice = configs.Configs.ToList().GetRange(beg, end - beg);
                var idleConnInds = curConnSlice.Select((val, ind) => new { val, ind })
                    .Where(z => z.val.SendFlag == false)
                    .Select(z => z.ind).ToList();
                idleConnInds.Shuffle();
                // var curMore = Util.SplitNumber(more, i, slaveCnt);
                for (int j = 0; j < moreSendConnList[i] && j < idleConnInds.Count; j++)
                {
                    configs.Configs[idleConnInds[j] + beg].SendFlag = true;
                }
                beg = end;
            }

            // Util.LogList("curSendConnList", curSendConnList);
            // Util.LogList("newSendConnList", newSendConnList);
            // Util.LogList("moreSendConnList", moreSendConnList);
            // Util.LogList("configs.Configs", configs.Configs.ToList());

            return configs;
        }
    }
}
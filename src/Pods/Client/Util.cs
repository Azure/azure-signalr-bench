using System;
using System.Collections.Generic;
using System.Text;

namespace Azure.SignalRBench.Client
{
    class Util
    {
        public static int[] GenerateConnectionID(int total, int startIndex, int count)
        {
            var rand = new Random(total);
            int[] globalConnectionIDs = new int[total];
            for (int i = 0; i < total; i++)
                globalConnectionIDs[i] = i;
            for (int i = 0; i < total; i++)
            {
                int tmp = globalConnectionIDs[i];
                int j = rand.Next(total);
                globalConnectionIDs[i] = globalConnectionIDs[j];
                globalConnectionIDs[j] = tmp;
            }
            int[] localConnectionIDs = new int[count];
            for (int i = 0; i < count; i++)
                localConnectionIDs[i] = globalConnectionIDs[startIndex + i];
            return localConnectionIDs;
        }

        public static string GenerateRandomData(int size)
        {
            var message = new byte[size];
            Random rnd = new Random();
            rnd.NextBytes(message);
            return Convert.ToBase64String(message).Substring(0, size);
        }
    }
}

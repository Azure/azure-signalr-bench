using System;

namespace Esendex.TokenBucket
{
    public abstract class Ticker
    {
        private class SystemTicker : Ticker
        {
            public override long Read()
            {
                return DateTime.Now.Ticks;
            }
        }

        private static readonly Ticker SystemTickerInstance = new SystemTicker();

        public abstract long Read();

        public static Ticker Default()
        {
            return SystemTickerInstance;
        }
    }
}
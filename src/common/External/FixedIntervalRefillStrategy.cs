using System;

namespace Esendex.TokenBucket
{
    /// <summary>
    /// A token bucket refill strategy that will provide N tokens for a token bucket to consume every T units of time.
    /// The tokens are refilled in bursts rather than at a fixed rate.  This refill strategy will never allow more than
    /// N tokens to be consumed during a window of time T.
    /// </summary>
    public class FixedIntervalRefillStrategy : IRefillStrategy
    {
        private readonly Ticker _ticker;
        private readonly long _numTokens;
        private readonly long _periodInTicks;
        private long _nextRefillTime;
        private readonly object _syncRoot = new object();

        /// <summary>Create a FixedIntervalRefillStrategy.</summary>
        /// <param name="ticker">A ticker to use to measure time.</param>
        /// <param name="numTokens">The number of tokens to add to the bucket every interval.</param>
        /// <param name="period">How often to refill the bucket.</param>
        public FixedIntervalRefillStrategy(Ticker ticker, long numTokens, TimeSpan period)
        {
            _ticker = ticker;
            _numTokens = numTokens;
            _periodInTicks = period.Ticks;
            _nextRefillTime = -1;
        }

        public long Refill()
        {
            lock (_syncRoot)
            {
                var now = _ticker.Read();
                if (now < _nextRefillTime)
                {
                    return 0;
                }
                var refillAmount = Math.Max((now - _nextRefillTime) / _periodInTicks, 1);
                _nextRefillTime += _periodInTicks * refillAmount;
                return _numTokens * refillAmount;
            }
        }
    }
}


using System;
using System.Threading;

namespace Esendex.TokenBucket
{
    /// <summary>
    /// Static utility methods pertaining to creating <see cref="ITokenBucket"/> instances.
    /// </summary>
    public static class TokenBuckets
    {
        public static Builder Construct()
        {
            return new Builder();
        }

        public class Builder
        {
            private long? _capacity;
            private IRefillStrategy _refillStrategy;
            private ISleepStrategy _sleepStrategy = YieldingSleepStrategyInstance;
            private readonly Ticker _ticker = Ticker.Default();

            public Builder WithCapacity(long numTokens)
            {
                if (numTokens <= 0)
                    throw new ArgumentOutOfRangeException("numTokens", "Must specify a positive number of tokens");
                _capacity = numTokens;
                return this;
            }

            /// <summary>Refill tokens at a fixed interval.</summary>
            public Builder WithFixedIntervalRefillStrategy(long refillTokens, TimeSpan period)
            {
                return WithRefillStrategy(new FixedIntervalRefillStrategy(_ticker, refillTokens, period));
            }

            /// <summary>Use a user defined refill strategy.</summary>
            public Builder WithRefillStrategy(IRefillStrategy refillStrategy)
            {
                if (refillStrategy == null)
                    throw new ArgumentNullException("refillStrategy");
                _refillStrategy = refillStrategy;
                return this;
            }

            /// <summary>Use a sleep strategy that will always attempt to yield the CPU to other processes.</summary>
            public Builder WithYieldingSleepStrategy()
            {
                return WithSleepStrategy(YieldingSleepStrategyInstance);
            }

            /// <summary>
            /// Use a sleep strategy that will not yield the CPU to other processes.  It will busy wait until more tokens become available.
            /// </summary>
            public Builder WithBusyWaitSleepStrategy()
            {
                return WithSleepStrategy(BusyWaitSleepStrategyInstance);
            }

            /// <summary>Use a user defined sleep strategy.</summary>
            public Builder WithSleepStrategy(ISleepStrategy sleepStrategy)
            {
                if (sleepStrategy == null)
                    throw new ArgumentNullException("sleepStrategy");
                _sleepStrategy = sleepStrategy;
                return this;
            }

            /// <summary>Build the token bucket.</summary>
            public ITokenBucket Build()
            {
                if (!_capacity.HasValue)
                    throw new InvalidOperationException("Must specify a capacity");

                return new TokenBucket(_capacity.Value, _refillStrategy, _sleepStrategy);
            }
        }

        private class YieldingSleepStrategy : ISleepStrategy
        {
            public void Sleep()
            {
                Thread.Sleep(0);
            }
        }

        private static readonly ISleepStrategy YieldingSleepStrategyInstance = new YieldingSleepStrategy();

        private class BusyWaitSleepStrategy : ISleepStrategy
        {
            public void Sleep()
            {
                Thread.SpinWait(1);
            }
        }

        private static readonly ISleepStrategy BusyWaitSleepStrategyInstance = new BusyWaitSleepStrategy();
    }
}
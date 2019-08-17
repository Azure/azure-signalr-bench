using System;
using System.Collections.Generic;
using System.Text;

namespace Common
{
    public class SemaphoreTokenBucketBuilder
    {
        int _refillTokens;
        int _capacity;
        TimeSpan _period;

        public static SemaphoreTokenBucketBuilder Builder()
        {
            return new SemaphoreTokenBucketBuilder();
        }

        public SemaphoreTokenBucketBuilder WithCapacity(int capacity)
        {
            _capacity = capacity;
            return this;
        }

        public SemaphoreTokenBucketBuilder WithRefill(int refillTokens, TimeSpan period)
        {
            _refillTokens = refillTokens;
            _period = period;
            return this;
        }

        public ISemaphoreTokenBucket Build()
        {
            if (_capacity == 0)
            {
                throw new InvalidOperationException("No capacity is specified");
            }
            if (_refillTokens == 0)
            {
                throw new InvalidOperationException("Refill token is zero");
            }
            if (_period == null)
            {
                throw new InvalidOperationException("No refilling period");
            }
            return new SemaphoreTokenBucket(_capacity, _refillTokens, _period);
        }
    }
}

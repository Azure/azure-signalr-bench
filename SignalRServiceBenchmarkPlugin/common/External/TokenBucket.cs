using System;

namespace Esendex.TokenBucket
{
    /// <summary>
    /// A token bucket implementation that is of a leaky bucket in the sense that it has a finite capacity and any added
    /// tokens that would exceed this capacity will "overflow" out of the bucket and are lost forever.
    ///
    /// In this implementation the rules for refilling the bucket are encapsulated in a provided <see cref="IRefillStrategy"/>
    /// instance.  Prior to attempting to consume any tokens the refill strategy will be consulted to see how many tokens
    /// should be added to the bucket.
    ///
    /// In addition in this implementation the method of yielding CPU control is encapsulated in the provided
    /// <see cref="ISleepStrategy"/> instance.  For high performance applications where tokens are being refilled incredibly quickly
    /// and an accurate bucket implementation is required, it may be useful to never yield control of the CPU and to instead
    /// busy wait.  This strategy allows the caller to make this decision for themselves instead of the library forcing a
    /// decision.
    /// </summary>
    internal class TokenBucket : ITokenBucket
    {
        private readonly long _capacity;
        private readonly IRefillStrategy _refillStrategy;
        private readonly ISleepStrategy _sleepStrategy;
        private long _size;
        private readonly object _syncRoot = new object();

        public TokenBucket(long capacity, IRefillStrategy refillStrategy, ISleepStrategy sleepStrategy)
        {
            _capacity = capacity;
            _refillStrategy = refillStrategy;
            _sleepStrategy = sleepStrategy;
            _size = 0;
        }

        /// <summary>
        /// Attempt to consume a single token from the bucket.  If it was consumed then <code>true</code>
        /// is returned, otherwise <code>false</code> is returned.
        /// </summary>
        /// <returns><code>true</code> if the tokens were consumed, <code>false</code> otherwise.</returns>
        public bool TryConsume()
        {
            return TryConsume(1);
        }

        /// <summary>
        /// Attempt to consume a specified number of tokens from the bucket.  If the tokens were consumed then <code>true</code>
        /// is returned, otherwise <code>false</code> is returned.
        /// </summary>
        /// <param name="numTokens">The number of tokens to consume from the bucket, must be a positive number.</param>
        /// <returns><code>true</code> if the tokens were consumed, <code>false</code> otherwise.</returns>
        public bool TryConsume(long numTokens)
        {
            if (numTokens <= 0)
                throw new ArgumentOutOfRangeException("numTokens", "Number of tokens to consume must be positive");
            if (numTokens > _capacity)
                throw new ArgumentOutOfRangeException("numTokens", "Number of tokens to consume must be less than the capacity of the bucket.");

            lock (_syncRoot)
            {
                // Give the refill strategy a chance to add tokens if it needs to, but beware of overflow
                var newTokens = Math.Min(_capacity, Math.Max(0, _refillStrategy.Refill()));
                _size = Math.Max(0, Math.Min(_size + newTokens, _capacity));

                if (numTokens > _size) return false;

                // Now try to consume some tokens
                _size -= numTokens;
                return true;
            }
        }

        /// <summary>
        /// Consume a single token from the bucket.  If no token is currently available then this method will block until a
        /// token becomes available.
        /// </summary>
        public void Consume()
        {
            Consume(1);
        }

        /// <summary>
        /// Consumes multiple tokens from the bucket.  If enough tokens are not currently available then this method will block
        /// </summary>
        /// <param name="numTokens">The number of tokens to consume from the bucket, must be a positive number.</param>
        public void Consume(long numTokens)
        {
            while (true) {
                if (TryConsume(numTokens)) {
                    break;
                }

                _sleepStrategy.Sleep();
            }
        }
    }
}

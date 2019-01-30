namespace Esendex.TokenBucket
{
    /// <summary>
    /// A token bucket is used for rate limiting access to a portion of code.
    /// 
    /// See <a href="http://en.wikipedia.org/wiki/Token_bucket">Token Bucket on Wikipedia</a>
    /// See <a href="http://en.wikipedia.org/wiki/Leaky_bucket">Leaky Bucket on Wikipedia</a>
    /// </summary>
    public interface ITokenBucket
    {
        /// <summary>
        /// Attempt to consume a single token from the bucket.  If it was consumed then <code>true</code>
        /// is returned, otherwise <code>false</code> is returned.
        /// </summary>
        /// <returns><code>true</code> if the tokens were consumed, <code>false</code> otherwise.</returns>
        bool TryConsume();

        /// <summary>
        /// Attempt to consume a specified number of tokens from the bucket.  If the tokens were consumed then <code>true</code>
        /// is returned, otherwise <code>false</code> is returned.
        /// </summary>
        /// <param name="numTokens">The number of tokens to consume from the bucket, must be a positive number.</param>
        /// <returns><code>true</code> if the tokens were consumed, <code>false</code> otherwise.</returns>
        bool TryConsume(long numTokens);

        /// <summary>
        /// Consume a single token from the bucket.  If no token is currently available then this method will block until a
        /// token becomes available.
        /// </summary>
        void Consume();

        /// <summary>
        /// Consumes multiple tokens from the bucket.  If enough tokens are not currently available then this method will block
        /// </summary>
        /// <param name="numTokens">The number of tokens to consume from the bucket, must be a positive number.</param>
        void Consume(long numTokens);
    }
}
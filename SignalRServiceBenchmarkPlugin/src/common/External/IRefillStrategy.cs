namespace Esendex.TokenBucket
{
    /// <summary>
    /// Encapsulation of a refilling strategy for a token bucket.
    /// </summary>
    public interface IRefillStrategy
    {
        /// <summary>Returns the number of tokens to add to the token bucket.</summary>
        /// <returns>The number of tokens to add to the token bucket.</returns>
        long Refill();
    }
}
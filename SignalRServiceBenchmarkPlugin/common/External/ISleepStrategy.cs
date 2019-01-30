namespace Esendex.TokenBucket
{
    /// <summary>
    /// Encapsulation of a strategy for relinquishing control of the CPU.
    /// </summary>
    public interface ISleepStrategy
    {
        /// <summary>
        /// Sleep for a short period of time to allow other threads and system processes to execute.
        /// </summary>
        void Sleep();
    }
}
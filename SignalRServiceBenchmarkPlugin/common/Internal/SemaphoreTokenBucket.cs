using System;
using System.Threading;
using System.Threading.Tasks;

namespace Common
{
    internal class SemaphoreTokenBucket : ISemaphoreTokenBucket, IDisposable
    {
        SemaphoreSlim _s;
        int _refillTokens;
        int _capacity;
        TimeSpan _period;
        Task _backgroudTask;
        bool _disposed = false;
        long _releasedCount;
        object _rootLock;
        CancellationTokenSource _cs;
        long _nextRefillTime;
        readonly long _periodInTicks;

        public SemaphoreTokenBucket(int capacity, int refillTokens, TimeSpan period)
        {
            _capacity = capacity;
            _refillTokens = refillTokens;
            _period = period;
            _periodInTicks = _period.Ticks;
            _nextRefillTime = 0;
            _s = new SemaphoreSlim(capacity);
            _rootLock = new object();
            _cs = new CancellationTokenSource();
            _backgroudTask = Task.Run(async () =>
            {
                while (!_cs.IsCancellationRequested)
                {
                    // refill
                    lock (_rootLock)
                    {
                        try
                        {
                            var nowTicks = DateTime.Now.Ticks;
                            if (nowTicks > _nextRefillTime)
                            {
                                var refillAmount = Math.Max((nowTicks - _nextRefillTime) / _periodInTicks, 1);
                                _nextRefillTime += _periodInTicks * refillAmount;
                                var refillCount = Math.Min(_capacity - _s.CurrentCount,
                                                           Math.Min(Interlocked.Read(ref _releasedCount), _refillTokens * refillAmount));
                                if (refillCount > 0)
                                {
                                    _s.Release((int)refillCount);
                                }
                                _releasedCount = 0;
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                        }
                    }
                    await Task.Delay(_period);
                }
            });
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _cs.Cancel();
            }
        }

        public long Release()
        {
            return Interlocked.Add(ref _releasedCount, 1);
        }

        public Task WaitAsync()
        {
            return _s.WaitAsync();
        }
    }
}

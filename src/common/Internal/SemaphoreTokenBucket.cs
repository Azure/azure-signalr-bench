﻿using System;
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
            // avoid the 1st filling fullfil the bucket
            _nextRefillTime = DateTime.Now.Ticks;
            var init = 0;
            _s = new SemaphoreSlim(init, capacity);
            // Smooth the burst in the beginning
            _ = Task.Run(async () =>
            {
                for (int i = init; i < capacity; i++)
                {
                    await Task.Delay(100);
                    _s.Release();
                }
            });
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

                                var releasedCount = Interlocked.Read(ref _releasedCount);
                                var holdingCount = _capacity - _s.CurrentCount;
                                var candidate = releasedCount > 0 ? Math.Min(releasedCount, holdingCount) : holdingCount;
                                var refillCount = Math.Min(candidate, _refillTokens * refillAmount);
                                if (refillCount > 0)
                                {
                                    var rel = _s.Release((int)refillCount);
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
                var holdingCount = _capacity - _s.CurrentCount;
                if (holdingCount > 0)
                {
                    _s.Release(holdingCount);
                }
                Console.WriteLine($"semaphore available count before destroyed: {_s.CurrentCount}");
                _s.Dispose();
                _disposed = true;
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

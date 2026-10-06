using System;
using System.Threading;
using System.Threading.Tasks;

namespace WbTnvedManager.Services
{
    public class RateLimiter
    {
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private DateTime _lastRequestTime = DateTime.MinValue;
        private int _delayMs;

        public int DelayMs
        {
            get => _delayMs;
            set => _delayMs = Math.Max(100, value);
        }

        public RateLimiter(int delayMs = 350)
        {
            _delayMs = Math.Max(100, delayMs);
        }

        public async Task ExecuteAsync(Func<Task> action, CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                var elapsed = (DateTime.UtcNow - _lastRequestTime).TotalMilliseconds;
                if (elapsed < _delayMs)
                {
                    var waitTime = (int)(_delayMs - elapsed);
                    await Task.Delay(waitTime, cancellationToken);
                }

                await action();
                _lastRequestTime = DateTime.UtcNow;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<T> ExecuteAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                var elapsed = (DateTime.UtcNow - _lastRequestTime).TotalMilliseconds;
                if (elapsed < _delayMs)
                {
                    var waitTime = (int)(_delayMs - elapsed);
                    await Task.Delay(waitTime, cancellationToken);
                }

                var result = await action();
                _lastRequestTime = DateTime.UtcNow;
                return result;
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}

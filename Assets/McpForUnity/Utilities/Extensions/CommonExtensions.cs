using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace ModelContextProtocol
{
    internal static class SemaphoreSlimExtensions
    {
        public static async Task<IDisposableLock> LockAsync(this SemaphoreSlim semaphore, CancellationToken cancellationToken = default)
        {
            await semaphore.WaitAsync(cancellationToken);
            return new IDisposableLock(semaphore);
        }

        public readonly struct IDisposableLock : IDisposable
        {
            private readonly SemaphoreSlim _semaphore;

            public IDisposableLock(SemaphoreSlim semaphore)
            {
                _semaphore = semaphore;
            }

            public void Dispose()
            {
                _semaphore?.Release();
            }
        }
    }

    internal static class ConcurrentQueueExtensions
    {
        public static void Clear<T>(this ConcurrentQueue<T> queue)
        {
                while (queue.TryDequeue(out _)) { }
        }
    }
}

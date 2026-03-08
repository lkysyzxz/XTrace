using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ModelContextProtocol.Protocol
{
    public interface IMessageReader<T>
    {
        Task<bool> WaitToReadAsync(CancellationToken cancellationToken = default);
        bool TryRead(out T item);
        IEnumerable<T> ReadAll();
    }

    public interface ITransport : IAsyncDisposable
    {
        string SessionId { get; }
        bool IsConnected { get; }
        IMessageReader<JsonRpcMessage> MessageReader { get; }
        Task SendMessageAsync(JsonRpcMessage message, CancellationToken cancellationToken = default);
    }

    internal class MessageQueueReader<T> : IMessageReader<T>
    {
        private readonly ConcurrentQueue<T> _queue;
        private readonly SemaphoreSlim _signal;
        private readonly Func<bool> _isCompleted;
        private readonly Func<Exception> _getError;

        public MessageQueueReader(ConcurrentQueue<T> queue, SemaphoreSlim signal, Func<bool> isCompleted, Func<Exception> getError)
        {
            _queue = queue;
            _signal = signal;
            _isCompleted = isCompleted;
            _getError = getError;
        }

        public async Task<bool> WaitToReadAsync(CancellationToken cancellationToken = default)
        {
            while (true)
            {
                if (!_queue.IsEmpty)
                    return true;

                if (_isCompleted())
                {
                    var error = _getError();
                    if (error != null)
                        throw new IOException("Channel completed with error", error);
                    return false;
                }

                try
                {
                    await _signal.WaitAsync(cancellationToken);
                }
                catch (ObjectDisposedException)
                {
                    return false;
                }
            }
        }

        public bool TryRead(out T item)
        {
            return _queue.TryDequeue(out item);
        }

        public IEnumerable<T> ReadAll()
        {
            while (true)
            {
                if (!_queue.IsEmpty)
                {
                    while (_queue.TryDequeue(out var item))
                    {
                        yield return item;
                    }
                }

                if (_isCompleted())
                {
                    yield break;
                }

                try
                {
                    _signal.Wait();
                }
                catch (ObjectDisposedException)
                {
                    yield break;
                }
            }
        }
    }

    internal static class MessageReaderExtensions
    {
        public static async Task ProcessMessagesAsync<T>(
            this IMessageReader<T> reader,
            Func<T, Task> processMessage,
            CancellationToken cancellationToken)
        {
            while (await reader.WaitToReadAsync(cancellationToken))
            {
                while (reader.TryRead(out var item))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await processMessage(item);
                }
            }
        }
    }
}

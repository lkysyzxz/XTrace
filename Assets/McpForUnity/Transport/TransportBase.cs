using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ModelContextProtocol.Protocol
{
    public abstract class TransportBase : ITransport
    {
        private readonly ConcurrentQueue<JsonRpcMessage> _messageQueue;
        private readonly SemaphoreSlim _messageSignal;
        private volatile int _state = StateInitial;
        private Exception _completeError;

        protected const int StateInitial = 0;
        protected const int StateConnected = 1;
        protected const int StateDisconnected = 2;

        protected string Name { get; }
        protected ILogger Logger { get; }

        public virtual string SessionId { get; protected set; }
        public bool IsConnected => _state == StateConnected;
        public IMessageReader<JsonRpcMessage> MessageReader { get; }

        protected TransportBase(string name, ILogger logger = null)
        {
            Name = name;
            Logger = logger ?? new UnityLoggerImpl();
            _messageQueue = new ConcurrentQueue<JsonRpcMessage>();
            _messageSignal = new SemaphoreSlim(0);
            MessageReader = new MessageQueueReader<JsonRpcMessage>(_messageQueue, _messageSignal, () => _state == StateDisconnected, () => _completeError);
        }

        public abstract Task SendMessageAsync(JsonRpcMessage message, CancellationToken cancellationToken = default);
        public abstract ValueTask DisposeAsync();

        protected async Task WriteMessageAsync(JsonRpcMessage message, CancellationToken cancellationToken = default)
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("Transport is not connected.");
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (Logger.IsEnabled(LogLevel.Debug))
            {
                var messageId = (message as JsonRpcMessageWithId)?.Id.ToString() ?? "(no id)";
                Logger.Log(LogLevel.Debug, $"{Name} received message with ID '{messageId}'");
            }

            _messageQueue.Enqueue(message);
            _messageSignal.Release();
        }

        protected void SetConnected()
        {
            while (true)
            {
                int state = _state;
                switch (state)
                {
                    case StateInitial:
                        if (Interlocked.CompareExchange(ref _state, StateConnected, StateInitial) == StateInitial)
                        {
                            return;
                        }
                        break;

                    case StateConnected:
                        return;

                    case StateDisconnected:
                        throw new IOException("Transport is already disconnected and can't be reconnected.");

                    default:
                        return;
                }
            }
        }

        protected void SetDisconnected(Exception error = null)
        {
            int state = _state;
            switch (state)
            {
                case StateInitial:
                case StateConnected:
                    _state = StateDisconnected;
                    _completeError = error;
                    _messageSignal.Release();
                    break;

                case StateDisconnected:
                    return;

                default:
                    break;
            }
        }

        protected void LogTransportConnectFailed(Exception exception)
        {
            Logger.Log(LogLevel.Error, $"{Name} transport connect failed", exception);
        }

        protected void LogTransportSendFailed(string messageId, Exception exception)
        {
            Logger.Log(LogLevel.Error, $"{Name} transport send failed for message ID '{messageId}'", exception);
        }

        protected void LogTransportReadMessagesFailed(Exception exception)
        {
            Logger.Log(LogLevel.Error, $"{Name} transport message reading failed", exception);
        }

        protected void LogTransportReadMessagesCancelled()
        {
            Logger.Log(LogLevel.Information, $"{Name} transport message reading canceled");
        }
    }
}

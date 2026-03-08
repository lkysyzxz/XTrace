using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ModelContextProtocol.Protocol
{
    public class SseEventWriter : IDisposable
    {
        private readonly Stream _stream;
        private readonly StreamWriter _writer;
        private static readonly byte[] NewLine = Encoding.UTF8.GetBytes("\n");
        private static readonly byte[] EventPrefix = Encoding.UTF8.GetBytes("event: ");
        private static readonly byte[] DataPrefix = Encoding.UTF8.GetBytes("data: ");
        private static readonly byte[] IdPrefix = Encoding.UTF8.GetBytes("id: ");

        public SseEventWriter(Stream stream)
        {
            _stream = stream ?? throw new ArgumentNullException(nameof(stream));
            _writer = new StreamWriter(stream, Encoding.UTF8, 1024, leaveOpen: true);
        }

        public async Task WriteAsync(string eventType, string data, string id = null, CancellationToken cancellationToken = default)
        {
            var buffer = new List<byte>();

            if (!string.IsNullOrEmpty(eventType))
            {
                buffer.AddRange(EventPrefix);
                buffer.AddRange(Encoding.UTF8.GetBytes(eventType));
                buffer.AddRange(NewLine);
            }

            if (!string.IsNullOrEmpty(id))
            {
                buffer.AddRange(IdPrefix);
                buffer.AddRange(Encoding.UTF8.GetBytes(id));
                buffer.AddRange(NewLine);
            }

            if (!string.IsNullOrEmpty(data))
            {
                foreach (var line in data.Split('\n'))
                {
                    buffer.AddRange(DataPrefix);
                    buffer.AddRange(Encoding.UTF8.GetBytes(line));
                    buffer.AddRange(NewLine);
                }
            }

            buffer.AddRange(NewLine);

            await _stream.WriteAsync(buffer.ToArray(), 0, buffer.Count, cancellationToken);
            await _stream.FlushAsync(cancellationToken);
        }

        public void Dispose()
        {
            _writer?.Dispose();
        }
    }

    public class SseItem<T>
    {
        public string EventType { get; set; }
        public string Id { get; set; }
        public T Data { get; set; }

        public static SseItem<T> Message(T data) => new SseItem<T> { EventType = "message", Data = data };
        public static SseItem<T> Prime() => new SseItem<T> { EventType = string.Empty, Data = default };
    }
}

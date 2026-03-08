using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ModelContextProtocol.Protocol
{
    public class SseParser
    {
        private readonly Stream _stream;
        private readonly StreamReader _reader;

        public SseParser(Stream stream)
        {
            _stream = stream ?? throw new ArgumentNullException(nameof(stream));
            _reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, 1024, leaveOpen: true);
        }

        public async IAsyncEnumerable<SseItem<string>> EnumerateAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            string eventType = null;
            string id = null;
            StringBuilder dataBuilder = new StringBuilder();

            while (!cancellationToken.IsCancellationRequested)
            {
                string line;
                try
                {
                    line = await _reader.ReadLineAsync();
                }
                catch (IOException)
                {
                    break;
                }

                if (line == null)
                {
                    if (dataBuilder.Length > 0)
                    {
                        yield return CreateItem(eventType, id, dataBuilder);
                    }
                    break;
                }

                if (string.IsNullOrEmpty(line))
                {
                    if (dataBuilder.Length > 0)
                    {
                        yield return CreateItem(eventType, id, dataBuilder);
                        eventType = null;
                        id = null;
                        dataBuilder.Clear();
                    }
                    continue;
                }

                if (line.StartsWith(":"))
                {
                    continue;
                }

                int colonIndex = line.IndexOf(':');
                string fieldName;
                string fieldValue;

                if (colonIndex < 0)
                {
                    fieldName = line;
                    fieldValue = string.Empty;
                }
                else if (colonIndex == 0)
                {
                    continue;
                }
                else
                {
                    fieldName = line.Substring(0, colonIndex);
                    if (colonIndex < line.Length - 1 && line[colonIndex + 1] == ' ')
                    {
                        fieldValue = line.Substring(colonIndex + 2);
                    }
                    else
                    {
                        fieldValue = line.Substring(colonIndex + 1);
                    }
                }

                switch (fieldName.ToLowerInvariant())
                {
                    case "event":
                        eventType = fieldValue;
                        break;
                    case "id":
                        id = fieldValue;
                        break;
                    case "data":
                        if (dataBuilder.Length > 0)
                        {
                            dataBuilder.Append('\n');
                        }
                        dataBuilder.Append(fieldValue);
                        break;
                    case "retry":
                    default:
                        break;
                }
            }
        }

        private SseItem<string> CreateItem(string eventType, string id, StringBuilder dataBuilder)
        {
            return new SseItem<string>
            {
                EventType = string.IsNullOrEmpty(eventType) ? "message" : eventType,
                Id = id,
                Data = dataBuilder.ToString()
            };
        }
    }
}

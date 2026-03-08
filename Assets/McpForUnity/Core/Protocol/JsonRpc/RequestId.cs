using System;
using Newtonsoft.Json;

namespace ModelContextProtocol.Protocol
{
    [JsonConverter(typeof(RequestIdConverter))]
    public readonly struct RequestId : IEquatable<RequestId>
    {
        public object Id { get; }

        public RequestId(object id)
        {
            Id = id;
        }

        public static RequestId FromInt(int id) => new RequestId(id);
        public static RequestId FromString(string id) => new RequestId(id);
        public static RequestId FromLong(long id) => new RequestId(id);

        public bool HasValue => Id != null;

        public override string ToString()
        {
            return Id?.ToString() ?? string.Empty;
        }

        public bool Equals(RequestId other)
        {
            return Equals(Id, other.Id);
        }

        public override bool Equals(object obj)
        {
            return obj is RequestId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Id?.GetHashCode() ?? 0;
        }

        public static bool operator ==(RequestId left, RequestId right) => left.Equals(right);
        public static bool operator !=(RequestId left, RequestId right) => !left.Equals(right);
    }

    public class RequestIdConverter : JsonConverter<RequestId>
    {
        public override RequestId ReadJson(JsonReader reader, Type objectType, RequestId existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return default;

            if (reader.TokenType == JsonToken.Integer)
            {
                var value = reader.Value;
                if (value is long l)
                    return RequestId.FromLong(l);
                if (value is int i)
                    return RequestId.FromInt(i);
                return new RequestId(Convert.ToInt64(value));
            }

            if (reader.TokenType == JsonToken.String)
            {
                return RequestId.FromString((string)reader.Value);
            }

            throw new JsonException($"Cannot convert {reader.TokenType} to RequestId");
        }

        public override void WriteJson(JsonWriter writer, RequestId value, JsonSerializer serializer)
        {
            if (value.Id == null)
            {
                writer.WriteNull();
            }
            else if (value.Id is int i)
            {
                writer.WriteValue(i);
            }
            else if (value.Id is long l)
            {
                writer.WriteValue(l);
            }
            else if (value.Id is string s)
            {
                writer.WriteValue(s);
            }
            else
            {
                writer.WriteValue(value.Id.ToString());
            }
        }
    }
}

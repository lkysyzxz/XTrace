using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ModelContextProtocol.Protocol
{
    public class Implementation
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonProperty("title", NullValueHandling = NullValueHandling.Ignore)]
        public string Title { get; set; }
    }

    public class Root
    {
        [JsonProperty("uri")]
        public string Uri { get; set; }

        [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
        public string Name { get; set; }

        [JsonProperty("_meta", NullValueHandling = NullValueHandling.Ignore)]
        public JObject Meta { get; set; }
    }

    public class ProgressToken
    {
        public object Value { get; }

        public ProgressToken(object value)
        {
            Value = value;
        }

        public static implicit operator ProgressToken(int value) => new ProgressToken(value);
        public static implicit operator ProgressToken(string value) => new ProgressToken(value);
    }

    public class ProgressTokenConverter : JsonConverter<ProgressToken>
    {
        public override ProgressToken ReadJson(JsonReader reader, System.Type objectType, ProgressToken existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Integer)
            {
                return new ProgressToken((long)reader.Value);
            }
            if (reader.TokenType == JsonToken.String)
            {
                return new ProgressToken((string)reader.Value);
            }
            throw new JsonException("Invalid progress token");
        }

        public override void WriteJson(JsonWriter writer, ProgressToken value, JsonSerializer serializer)
        {
            if (value.Value is int i)
                writer.WriteValue(i);
            else if (value.Value is long l)
                writer.WriteValue(l);
            else
                writer.WriteValue(value.Value?.ToString());
        }
    }
}

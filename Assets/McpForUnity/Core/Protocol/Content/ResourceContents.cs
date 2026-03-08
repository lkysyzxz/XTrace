using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ModelContextProtocol.Protocol
{
    [JsonConverter(typeof(ResourceContentsConverter))]
    public abstract class ResourceContents
    {
        [JsonProperty("uri")]
        public string Uri { get; set; }

        [JsonProperty("mimeType", NullValueHandling = NullValueHandling.Ignore)]
        public string MimeType { get; set; }
    }

    public class TextResourceContents : ResourceContents
    {
        [JsonProperty("text")]
        public string Text { get; set; }
    }

    public class BlobResourceContents : ResourceContents
    {
        [JsonProperty("blob")]
        public string Blob { get; set; }
    }

    public class ResourceContentsConverter : JsonConverter<ResourceContents>
    {
        public override ResourceContents ReadJson(JsonReader reader, System.Type objectType, ResourceContents existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var jo = JObject.Load(reader);
            
            if (jo["text"] != null)
            {
                return jo.ToObject<TextResourceContents>(serializer);
            }
            
            if (jo["blob"] != null)
            {
                return jo.ToObject<BlobResourceContents>(serializer);
            }

            return jo.ToObject<TextResourceContents>(serializer);
        }

        public override void WriteJson(JsonWriter writer, ResourceContents value, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("uri");
            writer.WriteValue(value.Uri);

            if (value.MimeType != null)
            {
                writer.WritePropertyName("mimeType");
                writer.WriteValue(value.MimeType);
            }

            switch (value)
            {
                case TextResourceContents text:
                    writer.WritePropertyName("text");
                    writer.WriteValue(text.Text);
                    break;

                case BlobResourceContents blob:
                    writer.WritePropertyName("blob");
                    writer.WriteValue(blob.Blob);
                    break;
            }

            writer.WriteEndObject();
        }
    }

    public class Resource
    {
        [JsonProperty("uri")]
        public string Uri { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description", NullValueHandling = NullValueHandling.Ignore)]
        public string Description { get; set; }

        [JsonProperty("mimeType", NullValueHandling = NullValueHandling.Ignore)]
        public string MimeType { get; set; }

        [JsonProperty("size", NullValueHandling = NullValueHandling.Ignore)]
        public long? Size { get; set; }

        [JsonProperty("_meta", NullValueHandling = NullValueHandling.Ignore)]
        public JObject Meta { get; set; }
    }

    public class ResourceTemplate
    {
        [JsonProperty("uriTemplate")]
        public string UriTemplate { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description", NullValueHandling = NullValueHandling.Ignore)]
        public string Description { get; set; }

        [JsonProperty("mimeType", NullValueHandling = NullValueHandling.Ignore)]
        public string MimeType { get; set; }

        [JsonProperty("_meta", NullValueHandling = NullValueHandling.Ignore)]
        public JObject Meta { get; set; }
    }
}

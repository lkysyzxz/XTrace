using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ModelContextProtocol.Protocol
{
    public enum Role
    {
        User,
        Assistant
    }

    public class Annotations
    {
        [JsonProperty("audience", NullValueHandling = NullValueHandling.Ignore)]
        public List<Role> Audience { get; set; }

        [JsonProperty("priority", NullValueHandling = NullValueHandling.Ignore)]
        public double? Priority { get; set; }
    }

    [JsonConverter(typeof(ContentBlockConverter))]
    public abstract class ContentBlock
    {
        [JsonProperty("type")]
        public abstract string Type { get; }

        [JsonProperty("annotations", NullValueHandling = NullValueHandling.Ignore)]
        public Annotations Annotations { get; set; }

        [JsonProperty("_meta", NullValueHandling = NullValueHandling.Ignore)]
        public JObject Meta { get; set; }
    }

    public class TextContentBlock : ContentBlock
    {
        public override string Type => "text";

        [JsonProperty("text")]
        public string Text { get; set; }
    }

    public class ImageContentBlock : ContentBlock
    {
        public override string Type => "image";

        [JsonProperty("data")]
        public string Data { get; set; }

        [JsonProperty("mimeType")]
        public string MimeType { get; set; }
    }

    public class AudioContentBlock : ContentBlock
    {
        public override string Type => "audio";

        [JsonProperty("data")]
        public string Data { get; set; }

        [JsonProperty("mimeType")]
        public string MimeType { get; set; }
    }

    public class ResourceLinkBlock : ContentBlock
    {
        public override string Type => "resource_link";

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
    }

    public class EmbeddedResourceBlock : ContentBlock
    {
        public override string Type => "resource";

        [JsonProperty("resource")]
        public ResourceContents Resource { get; set; }
    }

    public class ContentBlockConverter : JsonConverter<ContentBlock>
    {
        public override ContentBlock ReadJson(JsonReader reader, System.Type objectType, ContentBlock existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var jo = JObject.Load(reader);
            var type = jo["type"]?.ToString();

            switch (type)
            {
                case "text":
                    return jo.ToObject<TextContentBlock>(serializer);
                case "image":
                    return jo.ToObject<ImageContentBlock>(serializer);
                case "audio":
                    return jo.ToObject<AudioContentBlock>(serializer);
                case "resource":
                    return jo.ToObject<EmbeddedResourceBlock>(serializer);
                case "resource_link":
                    return jo.ToObject<ResourceLinkBlock>(serializer);
                default:
                    return jo.ToObject<TextContentBlock>(serializer);
            }
        }

        public override void WriteJson(JsonWriter writer, ContentBlock value, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("type");
            writer.WriteValue(value.Type);

            if (value.Annotations != null)
            {
                writer.WritePropertyName("annotations");
                serializer.Serialize(writer, value.Annotations);
            }

            if (value.Meta != null)
            {
                writer.WritePropertyName("_meta");
                value.Meta.WriteTo(writer);
            }

            switch (value)
            {
                case TextContentBlock text:
                    writer.WritePropertyName("text");
                    writer.WriteValue(text.Text);
                    break;

                case ImageContentBlock image:
                    writer.WritePropertyName("data");
                    writer.WriteValue(image.Data);
                    writer.WritePropertyName("mimeType");
                    writer.WriteValue(image.MimeType);
                    break;

                case AudioContentBlock audio:
                    writer.WritePropertyName("data");
                    writer.WriteValue(audio.Data);
                    writer.WritePropertyName("mimeType");
                    writer.WriteValue(audio.MimeType);
                    break;

                case ResourceLinkBlock link:
                    writer.WritePropertyName("uri");
                    writer.WriteValue(link.Uri);
                    writer.WritePropertyName("name");
                    writer.WriteValue(link.Name);
                    if (link.Description != null)
                    {
                        writer.WritePropertyName("description");
                        writer.WriteValue(link.Description);
                    }
                    if (link.MimeType != null)
                    {
                        writer.WritePropertyName("mimeType");
                        writer.WriteValue(link.MimeType);
                    }
                    if (link.Size.HasValue)
                    {
                        writer.WritePropertyName("size");
                        writer.WriteValue(link.Size.Value);
                    }
                    break;

                case EmbeddedResourceBlock embedded:
                    writer.WritePropertyName("resource");
                    serializer.Serialize(writer, embedded.Resource);
                    break;
            }

            writer.WriteEndObject();
        }
    }
}

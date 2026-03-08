using System;
using System.ComponentModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ModelContextProtocol.Protocol
{
    [JsonConverter(typeof(JsonRpcMessageConverter))]
    public abstract class JsonRpcMessage
    {
        [JsonProperty("jsonrpc")]
        public string JsonRpc { get; set; } = "2.0";

        [JsonIgnore]
        public JsonRpcMessageContext Context { get; set; }
    }

    public class JsonRpcMessageConverter : JsonConverter<JsonRpcMessage>
    {
        public override JsonRpcMessage ReadJson(JsonReader reader, Type objectType, JsonRpcMessage existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var jo = JObject.Load(reader);

            string jsonrpc = jo["jsonrpc"]?.ToString();
            if (jsonrpc != "2.0")
            {
                throw new JsonException("Invalid or missing jsonrpc version");
            }

            var idToken = jo["id"];
            var method = jo["method"]?.ToString();
            var errorToken = jo["error"];
            var resultToken = jo["result"];

            if (method != null)
            {
                var @params = jo["params"];

                if (idToken != null)
                {
                    return new JsonRpcRequest
                    {
                        Id = serializer.Deserialize<RequestId>(idToken.CreateReader()),
                        Method = method,
                        Params = @params
                    };
                }
                else
                {
                    return new JsonRpcNotification
                    {
                        Method = method,
                        Params = @params
                    };
                }
            }

            if (idToken != null)
            {
                if (errorToken != null)
                {
                    return new JsonRpcError
                    {
                        Id = serializer.Deserialize<RequestId>(idToken.CreateReader()),
                        Error = errorToken.ToObject<JsonRpcErrorDetail>(serializer)
                    };
                }

                if (resultToken != null)
                {
                    return new JsonRpcResponse
                    {
                        Id = serializer.Deserialize<RequestId>(idToken.CreateReader()),
                        Result = resultToken
                    };
                }

                throw new JsonException("Response must have either result or error");
            }

            throw new JsonException("Invalid JSON-RPC message format");
        }

        public override void WriteJson(JsonWriter writer, JsonRpcMessage value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            
            writer.WritePropertyName("jsonrpc");
            writer.WriteValue(value.JsonRpc);

            switch (value)
            {
                case JsonRpcRequest request:
                    WriteId(writer, request.Id);
                    writer.WritePropertyName("method");
                    writer.WriteValue(request.Method);
                    if (request.Params != null)
                    {
                        writer.WritePropertyName("params");
                        request.Params.WriteTo(writer);
                    }
                    break;

                case JsonRpcNotification notification:
                    writer.WritePropertyName("method");
                    writer.WriteValue(notification.Method);
                    if (notification.Params != null)
                    {
                        writer.WritePropertyName("params");
                        notification.Params.WriteTo(writer);
                    }
                    break;

                case JsonRpcResponse response:
                    WriteId(writer, response.Id);
                    writer.WritePropertyName("result");
                    if (response.Result != null)
                    {
                        response.Result.WriteTo(writer);
                    }
                    else
                    {
                        writer.WriteNull();
                    }
                    break;

                case JsonRpcError error:
                    WriteId(writer, error.Id);
                    writer.WritePropertyName("error");
                    serializer.Serialize(writer, error.Error);
                    break;

                default:
                    throw new JsonException($"Unknown JSON-RPC message type: {value.GetType()}");
            }

            writer.WriteEndObject();
        }

        private void WriteId(JsonWriter writer, RequestId id)
        {
            writer.WritePropertyName("id");
            if (id.Id is int i)
            {
                writer.WriteValue(i);
            }
            else if (id.Id is long l)
            {
                writer.WriteValue(l);
            }
            else if (id.Id is string s)
            {
                writer.WriteValue(s);
            }
            else if (id.Id != null)
            {
                writer.WriteValue(id.Id.ToString());
            }
            else
            {
                writer.WriteNull();
            }
        }
    }

    public abstract class JsonRpcMessageWithId : JsonRpcMessage
    {
        [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
        public RequestId Id { get; set; }
    }
}

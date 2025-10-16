using System.Text.Json;
using System.Text.Json.Serialization;

namespace Messbus.PubSub.Serialization;

public class JsonMessageSerializer : IMessageSerializer
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public T Deserialize<T>(byte[] data)
    {
        return JsonSerializer.Deserialize<T>(data, _jsonSerializerOptions);
    }

    public byte[] Serialize<T>(T message)
    {
        return JsonSerializer.SerializeToUtf8Bytes(message, _jsonSerializerOptions);
    }
}

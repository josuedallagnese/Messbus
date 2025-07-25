using System.Text.Json;
using System.Text.Json.Serialization;
using MessageBus.PubSub.Serialization;

namespace MessageBus.PubSub.Tests;

public class PubSubSerializerTests
{
    [Fact]
    public void SerializeAndDeserialize_ShouldReturnSameObject()
    {
        // Arrange
        var serializer = new PubSubSerializer();

        var originalMessage = new { Id = 1, Name = "Test" };

        // Act
        var serializedData = serializer.Serialize(originalMessage);

        var deserializedMessage = serializer.Deserialize<JsonDocument>(serializedData);

        // Assert
        Assert.Equal(originalMessage.Id, deserializedMessage.RootElement.GetProperty("id").GetInt32());
        Assert.Equal(originalMessage.Name, deserializedMessage.RootElement.GetProperty("name").GetString());
    }

    [Fact]
    public void SerializeAndDeserialize_ShouldReturnSameOriginalMessage()
    {
        // Arrange
        var serializer = new PubSubSerializer();
        var originalMessage = new OriginalMessage { Id = 1, Name = "Test" };

        // Act
        var serializedData = serializer.Serialize(originalMessage);
        var deserializedMessage = serializer.Deserialize<OriginalMessage>(serializedData);

        // Assert
        Assert.Equal(originalMessage.Id, deserializedMessage.Id);
        Assert.Equal(originalMessage.Name, deserializedMessage.Name);
    }
}

public class OriginalMessage
{
    [JsonPropertyName("Id")]
    public int Id { get; set; }

    [JsonPropertyName("Name")]
    public string Name { get; set; }
}

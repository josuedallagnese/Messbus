using System.Text.Json;
using System.Text.Json.Serialization;
using MessageBus.PubSub.Serialization;

namespace MessageBus.PubSub.Tests;

public class JsonMessageSerializerTests
{
    [Fact]
    public void SerializeAndDeserialize_ShouldReturnSameObject()
    {
        // Arrange
        var serializer = new JsonMessageSerializer();

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
        var serializer = new JsonMessageSerializer();
        var originalMessage = new OriginalMessage { Id = 1, Name = "Test" };

        // Act
        var serializedData = serializer.Serialize(originalMessage);
        var deserializedMessage = serializer.Deserialize<OriginalMessage>(serializedData);

        // Assert
        Assert.Equal(originalMessage.Id, deserializedMessage.Id);
        Assert.Equal(originalMessage.Name, deserializedMessage.Name);
    }

    [Fact]
    public void SerializeAndDeserialize_ShouldHandleSpecialTypes()
    {
        // Arrange
        var serializer = new JsonMessageSerializer();

        var now = DateTime.UtcNow;
        var guid = Guid.NewGuid();

        var specialMessage = new SpecialTypesMessage
        {
            Date = now,
            Guid = guid,
            DecimalValue = 123.45m,
            BoolValue = true
        };

        // Act
        var serializedData = serializer.Serialize(specialMessage);
        var deserializedMessage = serializer.Deserialize<SpecialTypesMessage>(serializedData);

        // Assert
        Assert.Equal(specialMessage.Date, deserializedMessage.Date, TimeSpan.FromMilliseconds(1));
        Assert.Equal(specialMessage.Guid, deserializedMessage.Guid);
        Assert.Equal(specialMessage.DecimalValue, deserializedMessage.DecimalValue);
        Assert.Equal(specialMessage.BoolValue, deserializedMessage.BoolValue);
    }

    [Fact]
    public void SerializeAndDeserialize_ShouldHandleSpecialCharacters()
    {
        // Arrange
        var serializer = new JsonMessageSerializer();

        var specialText = "Olá, mundo! Çãõüß€ 漢字 😀";
        var message = new SpecialCharactersMessage
        {
            Text = specialText
        };

        // Act
        var serializedData = serializer.Serialize(message);
        var deserializedMessage = serializer.Deserialize<SpecialCharactersMessage>(serializedData);

        // Assert
        Assert.Equal(specialText, deserializedMessage.Text);
    }

    public class SpecialCharactersMessage
    {
        public string Text { get; set; }
    }
}

public class OriginalMessage
{
    [JsonPropertyName("Id")]
    public int Id { get; set; }

    [JsonPropertyName("Name")]
    public string Name { get; set; }
}

public class SpecialTypesMessage
{
    public DateTime Date { get; set; }
    public Guid Guid { get; set; }
    public decimal DecimalValue { get; set; }
    public bool BoolValue { get; set; }
}

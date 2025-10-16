using Messbus.PubSub.Configuration;
using Messbus.PubSub.Internal;

namespace Messbus.PubSub.Tests.Configuration;

public class TopicIdTests
{
    [Fact]
    public void TopicId_When_Is_Unmanaged()
    {
        // Arrange
        var topicId = new TopicId("my-project", "my-topic");

        // Act
        var topicName = topicId.TopicName;
        var topic = topicId.Topic;

        // Assert
        Assert.Equal("my-topic", topicName.TopicId);
        Assert.Equal("projects/my-project/topics/my-topic", topic.TopicName.ToString());
        Assert.Null(topic.MessageRetentionDuration);
    }

    [Fact]
    public void TopicId_When_Is_Managed()
    {
        // Arrange
        var config = new PublishingConfiguration()
        {
            Prefix = "prefix",
            Topics = ["my-topic"],
            MessageRetentionDurationDays = 10
        };

        var topicId = new TopicId("my-project", "my-topic", config);

        // Act
        var topicName = topicId.TopicName;
        var topic = topicId.Topic;

        // Assert
        Assert.Equal("prefix.my-topic", topicName.TopicId);
        Assert.Equal("projects/my-project/topics/prefix.my-topic", topic.TopicName.ToString());
        Assert.NotNull(topic.MessageRetentionDuration);
        Assert.Equal(TimeSpan.FromDays(10), topic.MessageRetentionDuration.ToTimeSpan());
    }

    [Fact]
    public void TopicId_When_Is_Managed_Without_Prefix()
    {
        // Arrange
        var config = new PublishingConfiguration()
        {
            Topics = ["my-topic"],
            MessageRetentionDurationDays = 10
        };

        var topicId = new TopicId("my-project", "my-topic", config);

        // Act
        var topicName = topicId.TopicName;
        var topic = topicId.Topic;

        // Assert
        Assert.Equal("my-topic", topicName.TopicId);
        Assert.Equal("projects/my-project/topics/my-topic", topic.TopicName.ToString());
        Assert.NotNull(topic.MessageRetentionDuration);
        Assert.Equal(TimeSpan.FromDays(10), topic.MessageRetentionDuration.ToTimeSpan());
    }
}

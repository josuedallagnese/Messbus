using Messbus.PubSub.Configuration;

namespace Messbus.PubSub.Tests.Configuration;

public class PublishingConfigurationTests
{
    [Fact]
    public void PublishingConfiguration_When_Invalid_Topics()
    {
        // Arrange
        var config = new PublishingConfiguration();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => config.Validate());

        Assert.Equal("Topics must be provided when Publishing configuration section is configured.", exception.Message);
    }

    [Fact]
    public void PublishingConfiguration_When_Invalid_MessageRetentionDurationDays()
    {
        // Arrange
        var config = new PublishingConfiguration()
        {
            MessageRetentionDurationDays = 5,
            Topics = ["my-topic"]
        };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => config.Validate());

        Assert.Equal("MessageRetentionDurationDays must be provided if Publishing is configured and must be greater or equal than 7 days.", exception.Message);
    }

    [Fact]
    public void PublishingConfiguration_When_Invalid_TopicName()
    {
        // Arrange
        var config = new PublishingConfiguration()
        {
            MessageRetentionDurationDays = 5,
            Topics = ["another-topic"]
        };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => config.Validate("topic-name"));

        Assert.Equal("The topic 'topic-name' is not configured in Publishing.Topics configuration section", exception.Message);
    }
}

using MessageBus.PubSub.Configuration;

namespace MessageBus.PubSub.Tests.Configuration;

public class PubSubConfigurationTests
{
    [Fact]
    public void PubSubConfiguration_When_ProjectId_Is_Null_Or_Whitespace()
    {
        // Arrange
        var config = new PubSubConfiguration
        {
            ProjectId = "   "
        };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => config.Validate());

        Assert.Equal("The value cannot be an empty string or composed entirely of whitespace. (Parameter 'ProjectId')", exception.Message);
    }

    [Fact]
    public void PubSubConfiguration_When_Publishing_Is_Null_But_ResourceInitialization_Requires_It()
    {
        // Arrange
        var config = new PubSubConfiguration
        {
            ProjectId = "my-project",
            ResourceInitialization = ResourceInitialization.All,
            Publishing = null
        };

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => config.Validate());
        Assert.Equal("Value cannot be null. (Parameter 'Publishing')", exception.Message);
    }

    [Fact]
    public void PubSubConfiguration_When_Subscription_Is_Null_But_ResourceInitialization_Requires_It()
    {
        // Arrange
        var config = new PubSubConfiguration
        {
            ProjectId = "my-project",
            ResourceInitialization = ResourceInitialization.All,
            Publishing = new PublishingConfiguration
            {
                Topics = ["my-topic"],
                MessageRetentionDurationDays = 7
            },
            Subscription = null
        };

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => config.Validate());
        Assert.Equal("Value cannot be null. (Parameter 'Subscription')", exception.Message);
    }
}

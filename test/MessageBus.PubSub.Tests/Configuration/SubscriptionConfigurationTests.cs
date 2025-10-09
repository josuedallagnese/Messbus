using MessageBus.PubSub.Configuration;

namespace MessageBus.PubSub.Tests.Configuration;

public class SubscriptionConfigurationTests
{
    [Fact]
    public void SubscriptionConfiguration_When_Sufix_Is_Null_Or_Whitespace()
    {
        // Arrange
        var config = new SubscriptionConfiguration
        {
            Sufix = "   "
        };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => config.Validate());

        Assert.Equal("Sufix must be provided if Subscription is configured.", exception.Message);
    }

    [Fact]
    public void SubscriptionConfiguration_When_Invalid_MessageRetentionDurationDays()
    {
        // Arrange
        var config = new SubscriptionConfiguration
        {
            Sufix = "app",
            MessageRetentionDurationDays = 5
        };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => config.Validate());

        Assert.Equal($"MessageRetentionDurationDays must be provided if Subscription is configured and must be greater or equal than {SubscriptionConfiguration.MinimumAcceptableMessageRetentionDurationDays} days.", exception.Message);
    }

    [Fact]
    public void SubscriptionConfiguration_When_Invalid_AckDeadlineSeconds()
    {
        // Arrange
        var config = new SubscriptionConfiguration
        {
            Sufix = "app",
            MessageRetentionDurationDays = 7,
            AckDeadlineSeconds = 20
        };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => config.Validate());

        Assert.Equal($"AckDeadlineSeconds must be provided if Subscription is configured and must be greater or equal than {SubscriptionConfiguration.MinimumAcceptableAckDeadlineSeconds} seconds.", exception.Message);
    }

    [Fact]
    public void SubscriptionConfiguration_When_Invalid_MaxDeliveryAttempts()
    {
        // Arrange
        var config = new SubscriptionConfiguration
        {
            Sufix = "app",
            MessageRetentionDurationDays = 7,
            AckDeadlineSeconds = 30,
            MaxDeliveryAttempts = 3
        };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => config.Validate());

        Assert.Equal($"MaxDeliveryAttempts must be provided if Subscription is configured and must be greater or equal than {SubscriptionConfiguration.MinimumAcceptableMaxDeliveryAttempts}.", exception.Message);
    }

    [Fact]
    public void SubscriptionConfiguration_When_Invalid_MinBackoffSeconds()
    {
        // Arrange
        var config = new SubscriptionConfiguration
        {
            Sufix = "app",
            MessageRetentionDurationDays = 7,
            AckDeadlineSeconds = 30,
            MaxDeliveryAttempts = 5,
            MinBackoffSeconds = 4
        };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => config.Validate());

        Assert.Equal($"MinBackoffSeconds must be provided if Subscription is configured and must be greater or equal than {SubscriptionConfiguration.MinimumAcceptableMinBackoffSeconds} seconds.", exception.Message);
    }

    [Fact]
    public void SubscriptionConfiguration_When_Invalid_MaxBackoffSeconds()
    {
        // Arrange
        var config = new SubscriptionConfiguration
        {
            Sufix = "app",
            MessageRetentionDurationDays = 7,
            AckDeadlineSeconds = 30,
            MaxDeliveryAttempts = 5,
            MinBackoffSeconds = 10,
            MaxBackoffSeconds = 5
        };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => config.Validate());

        Assert.Equal($"MaxBackoffSeconds must be provided if Subscription is configured and must be greater or equal than {SubscriptionConfiguration.MinimumAcceptableMaxBackoffSeconds} seconds.", exception.Message);
    }

    [Fact]
    public void SubscriptionConfiguration_When_All_Values_Is_Valid()
    {
        // Arrange
        var config = new SubscriptionConfiguration
        {
            Sufix = "app",
            MessageRetentionDurationDays = 7,
            AckDeadlineSeconds = 30,
            MaxDeliveryAttempts = 5,
            MinBackoffSeconds = 10,
            MaxBackoffSeconds = 600
        };

        // Act & Assert
        config.Validate();

        Assert.Equal("app", config.Sufix);
        Assert.Equal(7, config.MessageRetentionDurationDays);
        Assert.Equal(30, config.AckDeadlineSeconds);
        Assert.Equal(5, config.MaxDeliveryAttempts);
        Assert.Equal(10, config.MinBackoffSeconds);
        Assert.Equal(600, config.MaxBackoffSeconds);
    }
}

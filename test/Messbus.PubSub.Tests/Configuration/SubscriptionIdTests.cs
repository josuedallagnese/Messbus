using Messbus.PubSub.Configuration;
using Messbus.PubSub.Internal;

namespace Messbus.PubSub.Tests.Configuration;

public class SubscriptionIdTests
{
    [Fact]
    public void SubscriptionId_When_Is_Unmanaged()
    {
        // Arrange
        var topicId = new TopicId("my-project", "my-topic");

        var subscriptionId = new SubscriptionId(topicId, "my-subscription");

        // Act
        var subscription = subscriptionId.Subscription;
        var deadLetterSubscription = subscriptionId.DeadLetterSubscription;
        var deadLetterTopic = subscriptionId.DeadLetterTopic;

        // Assert
        Assert.Equal("my-subscription", subscription.SubscriptionName.SubscriptionId);
        Assert.Equal("projects/my-project/subscriptions/my-subscription", subscription.SubscriptionName.ToString());
        Assert.Equal(topicId.TopicName, subscription.TopicAsTopicName);
        Assert.Null(deadLetterSubscription);
        Assert.Null(deadLetterTopic);
        Assert.Null(subscription.MessageRetentionDuration);
        Assert.Null(subscription.DeadLetterPolicy);
        Assert.Null(subscription.RetryPolicy);
    }

    [Fact]
    public void SubscriptionId_When_Is_Managed()
    {
        // Arrange
        var config = new SubscriptionConfiguration()
        {
            Sufix = "app",
            MessageRetentionDurationDays = 7,
            AckDeadlineSeconds = 30,
            MaxDeliveryAttempts = 5,
            MinBackoffSeconds = 10,
            MaxBackoffSeconds = 600,
            MaxOutstandingByteCount = 1024,
            MaxOutstandingElementCount = 100
        };

        var topicId = new TopicId("my-project", "my-topic");

        var subscriptionId = new SubscriptionId(topicId, config);

        // Act
        var subscription = subscriptionId.Subscription;
        var deadLetterSubscription = subscriptionId.DeadLetterSubscription;
        var deadLetterTopic = subscriptionId.DeadLetterTopic;

        // Assert
        Assert.Equal("my-topic.app", subscription.SubscriptionName.SubscriptionId);
        Assert.Equal("projects/my-project/subscriptions/my-topic.app", subscription.SubscriptionName.ToString());
        Assert.Equal("my-topic.app.dl", deadLetterSubscription.SubscriptionName.SubscriptionId);
        Assert.Equal("projects/my-project/subscriptions/my-topic.app.dl", deadLetterSubscription.SubscriptionName.ToString());
        Assert.Equal("projects/my-project/topics/my-topic.app.dl", deadLetterTopic.Name);
        Assert.Equal(topicId.TopicName, subscription.TopicAsTopicName);
        Assert.NotNull(subscription.MessageRetentionDuration);
        Assert.Equal(TimeSpan.FromDays(7), subscription.MessageRetentionDuration.ToTimeSpan());
        Assert.NotNull(subscription.DeadLetterPolicy);
        Assert.Equal(subscription.DeadLetterPolicy.DeadLetterTopic, deadLetterTopic.Name);
        Assert.Equal(5, subscription.DeadLetterPolicy.MaxDeliveryAttempts);
        Assert.NotNull(subscription.RetryPolicy);
        Assert.Equal(TimeSpan.FromSeconds(10), subscription.RetryPolicy.MinimumBackoff.ToTimeSpan());
        Assert.Equal(TimeSpan.FromSeconds(600), subscription.RetryPolicy.MaximumBackoff.ToTimeSpan());
        Assert.NotNull(deadLetterTopic.MessageRetentionDuration);
        Assert.Equal(TimeSpan.FromDays(7), deadLetterTopic.MessageRetentionDuration.ToTimeSpan());
    }
}

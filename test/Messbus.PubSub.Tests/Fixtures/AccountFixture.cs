using Messbus.PubSub.Internal;
using Microsoft.Extensions.Logging;

namespace Messbus.PubSub.Tests.Fixtures;

public abstract class AccountFixture
{
    public abstract Task Initialize();

    protected static async Task Cleanup(EnvironmentId environmentId, SubscriptionId subscriptionId, ILogger logger)
    {
        logger.LogInformation("Cleaning up resources from {SubscriptionId}...", subscriptionId);

        await environmentId.DeleteSubscriptions(subscriptionId, logger);
        await environmentId.DeleteTopics(subscriptionId, logger);
    }

    protected static async Task InitializeUnmanaged(EnvironmentId environmentId, SubscriptionId subscriptionId, ILogger logger)
    {
        await Cleanup(environmentId, subscriptionId, logger);

        logger.LogInformation("Create unmanaged topic and subscription...");

        await environmentId.CreateTopic(subscriptionId.TopicId.Topic, logger, default);
        await environmentId.CreateSubscription(subscriptionId.Subscription, logger, default);
    }
}

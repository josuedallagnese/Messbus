using Google.Cloud.PubSub.V1;
using Google.Cloud.ResourceManager.V3;
using Google.Protobuf.WellKnownTypes;
using MessageBus.PubSub.Configuration;

namespace MessageBus.PubSub.Internal;

public class SubscriptionId
{
    public TopicId TopicId { get; }
    public Subscription Subscription { get; }
    public Topic DeadLetterTopic { get; }
    public Subscription DeadLetterSubscription { get; }


    internal SubscriptionId(TopicId topicId, string subscription)
    {
        TopicId = topicId ?? throw new ArgumentNullException(nameof(topicId));

        var subscriptionName = GetUnmanagedSubscriptionName(topicId, subscription);
        Subscription = GetUnmanagedSubscription(topicId, subscriptionName);
    }

    internal SubscriptionId(TopicId topicId, SubscriptionConfiguration config)
    {
        TopicId = topicId ?? throw new ArgumentNullException(nameof(topicId));
        ArgumentNullException.ThrowIfNull(config, nameof(config));

        config.Validate();

        var subscriptionName = GetSubscriptionName(topicId, config);
        DeadLetterTopic = GetDeadLetterTopic(config, subscriptionName);
        DeadLetterSubscription = GetDeadLetterSubscription(DeadLetterTopic, config, subscriptionName);
        Subscription = GetSubscription(topicId, config, subscriptionName, DeadLetterTopic);
    }

    private static Subscription GetUnmanagedSubscription(TopicId topicId, SubscriptionName subscriptionName)
    {
        var subscription = new Subscription()
        {
            SubscriptionName = subscriptionName,
            TopicAsTopicName = topicId.TopicName
        };

        return subscription;
    }

    private static Subscription GetSubscription(TopicId topicId, SubscriptionConfiguration config, SubscriptionName subscriptionName, Topic deadLetterTopic)
    {
        var subscription = new Subscription
        {
            SubscriptionName = subscriptionName,
            TopicAsTopicName = topicId.TopicName,
            Detached = false,
            RetainAckedMessages = false,
            MessageRetentionDuration = Duration.FromTimeSpan(TimeSpan.FromDays(config.MessageRetentionDurationDays)),
            AckDeadlineSeconds = config.AckDeadlineSeconds,
            ExpirationPolicy = new ExpirationPolicy(),

            DeadLetterPolicy = new DeadLetterPolicy
            {
                DeadLetterTopic = deadLetterTopic.Name,
                MaxDeliveryAttempts = config.MaxDeliveryAttempts
            },

            RetryPolicy = new RetryPolicy()
            {
                MinimumBackoff = Duration.FromTimeSpan(TimeSpan.FromSeconds(config.MinBackoffSeconds)),
                MaximumBackoff = Duration.FromTimeSpan(TimeSpan.FromSeconds(config.MaxBackoffSeconds))
            }
        };

        return subscription;
    }

    private static Topic GetDeadLetterTopic(SubscriptionConfiguration config, SubscriptionName subscriptionName)
    {
        var topic = new Topic()
        {
            TopicName = new(subscriptionName.ProjectId, $"{subscriptionName.SubscriptionId}.dl")
        };

        if (config is not null)
            topic.MessageRetentionDuration = Duration.FromTimeSpan(TimeSpan.FromDays(config.MessageRetentionDurationDays));

        return topic;
    }

    public static Subscription GetDeadLetterSubscription(Topic deadLetterTopic, SubscriptionConfiguration config, SubscriptionName subscriptionName)
    {
        var deadLetterSubscriptionName = new SubscriptionName(subscriptionName.ProjectId, $"{subscriptionName.SubscriptionId}.dl");

        var subscription = new Subscription()
        {
            SubscriptionName = deadLetterSubscriptionName,
            TopicAsTopicName = deadLetterTopic.TopicName
        };

        if (config is not null)
        {
            subscription.Detached = false;
            subscription.RetainAckedMessages = false;
            subscription.MessageRetentionDuration = Duration.FromTimeSpan(TimeSpan.FromDays(config.MessageRetentionDurationDays));
            subscription.AckDeadlineSeconds = config.AckDeadlineSeconds;
            subscription.ExpirationPolicy = new ExpirationPolicy();
            subscription.RetryPolicy = new RetryPolicy()
            {
                MinimumBackoff = Duration.FromTimeSpan(TimeSpan.FromSeconds(config.MinBackoffSeconds)),
                MaximumBackoff = Duration.FromTimeSpan(TimeSpan.FromSeconds(config.MaxBackoffSeconds))
            };
        }

        return subscription;
    }

    private SubscriptionName GetSubscriptionName(TopicId topicId, SubscriptionConfiguration config)
    {
        return new SubscriptionName(topicId.TopicName.ProjectId, $"{topicId.TopicName.TopicId}.{config.Sufix}");
    }

    private SubscriptionName GetUnmanagedSubscriptionName(TopicId topicId, string subscription)
    {
        return new SubscriptionName(topicId.TopicName.ProjectId, subscription);
    }

    private async Task<string> GetProjectNumber()
    {
        var client = await ProjectsClient.CreateAsync();

        var response = client.SearchProjects(new SearchProjectsRequest
        {
            Query = $"id:{Subscription.SubscriptionName.ProjectId}"
        });

        var project = response.FirstOrDefault();

        return project.ProjectName.ProjectId;
    }

    public async Task<string> GetServiceAccount()
    {
        var projectNumber = await GetProjectNumber();

        return $"serviceAccount:service-{projectNumber}@gcp-sa-pubsub.iam.gserviceaccount.com";
    }

    public override string ToString() => Subscription.SubscriptionName.SubscriptionId.ToString();
}

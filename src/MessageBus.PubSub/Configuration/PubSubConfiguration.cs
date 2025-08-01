using Google.Apis.Auth.OAuth2;
using Grpc.Auth;
using Grpc.Core;
using Microsoft.Extensions.Configuration;

namespace MessageBus.PubSub.Configuration;

public class PubSubConfiguration
{
    public string Alias { get; set; }
    public string ProjectId { get; set; }
    public string JsonCredentials { get; set; }
    public ResourceInitialization ResourceInitialization { get; set; } = ResourceInitialization.All;
    public bool UseEmulator { get; set; }

    public PublishingConfiguration Publishing { get; set; }
    public SubscriptionConfiguration Subscription { get; set; }

    public PubSubConfiguration()
    {
    }

    public PubSubConfiguration(string alias, IConfiguration configuration)
    {
        Alias = alias;

        var configurationSectionName = string.IsNullOrWhiteSpace(alias) ?
            "MessageBus:PubSub" :
            $"MessageBus:PubSub:{alias}";

        var configurationSection = configuration.GetRequiredSection(configurationSectionName);

        ProjectId = configurationSection.GetValue<string>(nameof(ProjectId));
        JsonCredentials = configurationSection.GetValue<string>(nameof(JsonCredentials));
        Publishing = configurationSection.GetSection(nameof(Publishing)).Get<PublishingConfiguration>();
        Subscription = configurationSection.GetSection(nameof(Subscription)).Get<SubscriptionConfiguration>();
        UseEmulator = configurationSection.GetValue<bool>(nameof(UseEmulator));
        ResourceInitialization = configurationSection.GetValue<ResourceInitialization>(nameof(ResourceInitialization));
    }

    public ChannelCredentials GetChannelCredentials()
    {
        if (UseEmulator)
            return null;

        GoogleCredential credential;

        if (string.IsNullOrWhiteSpace(JsonCredentials))
            credential = GoogleCredential.GetApplicationDefault();
        else
            credential = GoogleCredential.FromJson(JsonCredentials);

        return credential.ToChannelCredentials();
    }

    public TopicId GetTopicId(string topicId)
    {
        ArgumentNullException.ThrowIfNull(Publishing, nameof(Publishing));

        if (!Publishing.HasTopic(topicId))
            throw new ArgumentException($"The topic {topicId} is not configured in Publishing section");

        return new TopicId(ProjectId, topicId, this);
    }

    public SubscriptionId GetSubscriptionId(string topic)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topic, nameof(topic));
        ArgumentNullException.ThrowIfNull(Subscription, nameof(Subscription));

        var topicId = GetTopicId(topic);

        return new SubscriptionId(ProjectId, topicId, this);
    }

    public bool ShouldInitializeTopics()
    {
        if (ResourceInitialization == ResourceInitialization.All ||
            ResourceInitialization == ResourceInitialization.TopicsOnly)
            return true;

        return false;
    }

    public bool ShouldInitializeSubscriptions()
    {
        if (ResourceInitialization == ResourceInitialization.All ||
            ResourceInitialization == ResourceInitialization.SubscriptionsOnly)
            return true;

        return false;
    }

    public void Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ProjectId, nameof(ProjectId));

        if (Publishing is not null)
        {
            if (Publishing.MessageRetentionDurationDays < 7)
                throw new ArgumentException($"{nameof(Publishing.MessageRetentionDurationDays)} must be provided if {nameof(Publishing)} is configured and must be greater or equal than 7 days.");

            if (Publishing.Topics is null || Publishing.Topics.Count == 0)
                throw new ArgumentException($"{nameof(Publishing.Topics)} must be provided and non-empty if {nameof(Publishing)} is configured.");
        }

        if (Subscription is not null)
        {
            if (string.IsNullOrWhiteSpace(Subscription.Sufix))
                throw new ArgumentException($"{nameof(Subscription.Sufix)} must be provided if {nameof(Subscription)} is configured.");

            if (Publishing.MessageRetentionDurationDays < 7)
                throw new ArgumentException($"{nameof(Publishing.MessageRetentionDurationDays)} must be provided if {nameof(Publishing)} is configured and must be greater or equal than 7 days.");
        }
    }
}

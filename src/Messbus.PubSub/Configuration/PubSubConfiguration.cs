using System.Collections.Concurrent;
using Google.Apis.Auth.OAuth2;
using Grpc.Auth;
using Grpc.Core;
using Messbus.PubSub.Internal;
using Microsoft.Extensions.Configuration;

namespace Messbus.PubSub.Configuration;

public class PubSubConfiguration
{
    private readonly ConcurrentDictionary<string, TopicId> _topicCache = new();
    private readonly ConcurrentDictionary<string, SubscriptionId> _subscriptionCache = new();
    private readonly ConcurrentDictionary<string, EnvironmentId> _environmentCache = new();

    public string Alias { get; set; }
    public string ProjectId { get; set; }
    public string JsonCredentials { get; set; }
    public ResourceInitialization ResourceInitialization { get; set; } = ResourceInitialization.All;
    public bool UseEmulator { get; set; }
    public bool VerbosityMode { get; set; } = false;

    public PublishingConfiguration Publishing { get; set; }
    public SubscriptionConfiguration Subscription { get; set; }

    public PubSubConfiguration()
    {
    }

    public PubSubConfiguration(IConfiguration configuration)
        : this(null, configuration)
    {
    }

    public PubSubConfiguration(string alias, IConfiguration configuration)
    {
        Alias = alias;

        var configurationSectionName = string.IsNullOrWhiteSpace(alias) ?
            "Messbus:PubSub" :
            $"Messbus:PubSub:{alias}";

        var configurationSection = configuration.GetRequiredSection(configurationSectionName);

        ProjectId = configurationSection.GetValue<string>(nameof(ProjectId));
        JsonCredentials = configurationSection.GetValue<string>(nameof(JsonCredentials));
        Publishing = configurationSection.GetSection(nameof(Publishing)).Get<PublishingConfiguration>();
        Subscription = configurationSection.GetSection(nameof(Subscription)).Get<SubscriptionConfiguration>();
        UseEmulator = configurationSection.GetValue<bool>(nameof(UseEmulator));
        ResourceInitialization = configurationSection.GetValue(nameof(ResourceInitialization), ResourceInitialization.All);
        VerbosityMode = configurationSection.GetValue(nameof(VerbosityMode), false);
    }

    internal ChannelCredentials GetChannelCredentials()
    {
        var credential = GetGoogleCredential();

        return credential.ToChannelCredentials();
    }

    internal EnvironmentId GetEnvironmentId(string topic, string subscription = null)
    {
        var key = $"{topic}::{subscription}";

        return _environmentCache.GetOrAdd(key, _ =>
        {
            var unmanaged = !string.IsNullOrWhiteSpace(subscription);
            var channelCredentials = GetChannelCredentials();
            var flowControl = Subscription?.GetFlowControlSettings();

            var environentId = new EnvironmentId(
                UseEmulator,
                unmanaged,
                VerbosityMode,
                channelCredentials,
                flowControl);

            return environentId;
        });
    }

    internal TopicId GetTopicId(string topic, string subscription = null)
    {
        Guard.ThrowIfNullOrWhiteSpace(topic, nameof(topic));

        return _topicCache.GetOrAdd(topic, t =>
        {
            var environmentId = GetEnvironmentId(topic, subscription);

            if (environmentId.Unmanaged)
                return new TopicId(ProjectId, t);

            return new TopicId(ProjectId, t, Publishing);
        });
    }

    internal SubscriptionId GetSubscriptionId(string topic, string subscription = null)
    {
        Guard.ThrowIfNullOrWhiteSpace(topic, nameof(topic));

        var key = $"{topic}::{subscription}";

        return _subscriptionCache.GetOrAdd(key, _ =>
        {
            var topicId = GetTopicId(topic, subscription);
            var environentId = GetEnvironmentId(topic, subscription);

            if (environentId.Unmanaged)
                return new SubscriptionId(topicId, subscription);

            return new SubscriptionId(topicId, Subscription);
        });
    }

    internal bool ShouldInitializeTopics()
    {
        if (ResourceInitialization == ResourceInitialization.All ||
            ResourceInitialization == ResourceInitialization.TopicsOnly)
            return true;

        return false;
    }

    internal bool ShouldInitializeSubscriptions()
    {
        if (ResourceInitialization == ResourceInitialization.All ||
            ResourceInitialization == ResourceInitialization.SubscriptionsOnly)
            return true;

        return false;
    }

    internal void Validate()
    {
        Guard.ThrowIfNullOrWhiteSpace(ProjectId, nameof(ProjectId));

        if (ShouldInitializeTopics())
        {
            ArgumentNullException.ThrowIfNull(Publishing, nameof(Publishing));
            Publishing.Validate();
        }

        if (ShouldInitializeSubscriptions())
        {
            ArgumentNullException.ThrowIfNull(Subscription, nameof(Subscription));
            Subscription.Validate();
        }
    }

    private GoogleCredential GetGoogleCredential()
    {
        if (UseEmulator)
            return null;

        GoogleCredential credential;

        if (string.IsNullOrWhiteSpace(JsonCredentials))
            credential = GoogleCredential.GetApplicationDefault();
        else
            credential = GoogleCredential.FromJson(JsonCredentials);

        return credential;
    }
}

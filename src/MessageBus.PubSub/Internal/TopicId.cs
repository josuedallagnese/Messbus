using Google.Cloud.PubSub.V1;
using Google.Protobuf.WellKnownTypes;
using MessageBus.PubSub.Configuration;

namespace MessageBus.PubSub.Internal;

public class TopicId
{
    public TopicName TopicName { get; }
    public Topic Topic { get; }

    internal TopicId(string projectId, string topic, PublishingConfiguration config)
    {
        Guard.ThrowIfNullOrWhiteSpace(projectId, nameof(projectId));
        Guard.ThrowIfNullOrWhiteSpace(projectId, nameof(projectId));
        Guard.ThrowIfNullOrWhiteSpace(topic, nameof(topic));
        ArgumentNullException.ThrowIfNull(config, nameof(PublishingConfiguration));

        config.Validate(topic);

        TopicName = string.IsNullOrWhiteSpace(config.Prefix) ?
            new TopicName(projectId, topic) :
            new TopicName(projectId, $"{config.Prefix}.{topic}");

        Topic = GetTopic(TopicName, config);
    }

    internal TopicId(string projectId, string topic)
    {
        Guard.ThrowIfNullOrWhiteSpace(projectId, nameof(projectId));
        Guard.ThrowIfNullOrWhiteSpace(topic, nameof(topic));

        TopicName = new TopicName(projectId, topic);
        Topic = GetUnmanagedTopic(TopicName);
    }

    private static Topic GetUnmanagedTopic(TopicName topicName)
    {
        var topic = new Topic()
        {
            TopicName = topicName
        };

        return topic;
    }

    private static Topic GetTopic(TopicName topicName, PublishingConfiguration config)
    {
        var topic = new Topic()
        {
            TopicName = topicName,
            MessageRetentionDuration = Duration.FromTimeSpan(TimeSpan.FromDays(config.MessageRetentionDurationDays))
        };

        return topic;
    }

    public override string ToString() => TopicName.TopicId.ToString();
}

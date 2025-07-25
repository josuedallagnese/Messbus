using Google.Cloud.PubSub.V1;
using Google.Protobuf.WellKnownTypes;

namespace MessageBus.PubSub.Configuration;

public class TopicId(string projectId, string topic, PubSubConfiguration pubSubConfiguration)
{
    public PubSubConfiguration PubSubConfiguration { get; } = pubSubConfiguration;

    public TopicName TopicName => new(projectId,
        !string.IsNullOrWhiteSpace(PubSubConfiguration.Publishing.Prefix) ? $"{PubSubConfiguration.Publishing.Prefix}.{topic}" : topic);

    public Topic GetTopic()
    {
        var topic = new Topic()
        {
            TopicName = TopicName,
            MessageRetentionDuration = Duration.FromTimeSpan(TimeSpan.FromDays(PubSubConfiguration.Publishing.MessageRetentionDurationDays))
        };

        return topic;
    }

    public override string ToString() => TopicName.TopicId.ToString();
}

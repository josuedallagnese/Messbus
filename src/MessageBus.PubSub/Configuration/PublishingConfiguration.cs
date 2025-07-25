namespace MessageBus.PubSub.Configuration;

public class PublishingConfiguration
{
    public string Prefix { get; set; }
    public int MessageRetentionDurationDays { get; set; } = 7;
    public List<string> Topics { get; set; } = [];

    public bool HasTopic(string topic)
    {
        return Topics.Any(t => t == topic);
    }
}

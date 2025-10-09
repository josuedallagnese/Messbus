namespace MessageBus.PubSub.Configuration;

public class PublishingConfiguration
{
    public const int DefaultMessageRetentionDurationDays = 7;

    public string Prefix { get; set; }
    public int MessageRetentionDurationDays { get; set; } = DefaultMessageRetentionDurationDays;
    public List<string> Topics { get; set; } = new();

    public bool HasTopic(string topic)
    {
        return Topics.Any(t => t == topic);
    }

    internal void Validate(string topic = null)
    {
        if (Topics is null || Topics.Count == 0)
            throw new ArgumentException("Topics must be provided when Publishing configuration section is configured.");

        if (!string.IsNullOrWhiteSpace(topic) && !HasTopic(topic))
            throw new ArgumentException($"The topic '{topic}' is not configured in Publishing.Topics configuration section");

        if (MessageRetentionDurationDays < DefaultMessageRetentionDurationDays)
            throw new ArgumentException($"MessageRetentionDurationDays must be provided if Publishing is configured and must be greater or equal than {DefaultMessageRetentionDurationDays} days.");
    }
}

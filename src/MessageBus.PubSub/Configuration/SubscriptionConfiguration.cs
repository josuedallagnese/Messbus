namespace MessageBus.PubSub.Configuration;

public class SubscriptionConfiguration
{
    public string Sufix { get; set; }
    public int MessageRetentionDurationDays { get; set; } = 7;
    public int AckDeadlineSeconds { get; set; } = 30;
    public int MaxDeliveryAttempts { get; set; } = 5;
    public int MinBackoffSeconds { get; set; } = 10;
    public int MaxBackoffSeconds { get; set; } = 600;
}

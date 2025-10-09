using Google.Api.Gax;
using Google.Cloud.PubSub.V1;

namespace MessageBus.PubSub.Configuration;

public class SubscriptionConfiguration
{
    public const int DefaultMessageRetentionDurationDays = 7;
    public const int DefaultAckDeadlineSeconds = 120;
    public const int DefaultMaxDeliveryAttempts = 5;
    public const int DefaultMinBackoffSeconds = 5;
    public const int DefaultMaxBackoffSeconds = 600;

    public const int MinimumAcceptableMessageRetentionDurationDays = 7;
    public const int MinimumAcceptableAckDeadlineSeconds = 30;
    public const int MinimumAcceptableMaxDeliveryAttempts = 5;
    public const int MinimumAcceptableMinBackoffSeconds = 5;
    public const int MinimumAcceptableMaxBackoffSeconds = 600;

    public string Sufix { get; set; }
    public int MessageRetentionDurationDays { get; set; } = DefaultMessageRetentionDurationDays;
    public int AckDeadlineSeconds { get; set; } = DefaultAckDeadlineSeconds;
    public int MaxDeliveryAttempts { get; set; } = DefaultMaxDeliveryAttempts;
    public int MinBackoffSeconds { get; set; } = DefaultMinBackoffSeconds;
    public int MaxBackoffSeconds { get; set; } = DefaultMaxBackoffSeconds;
    public long? MaxOutstandingElementCount { get; set; }
    public long? MaxOutstandingByteCount { get; set; }

    internal FlowControlSettings GetFlowControlSettings()
    {
        if (MaxOutstandingElementCount.HasValue || MaxOutstandingByteCount.HasValue)
        {
            return new FlowControlSettings(MaxOutstandingElementCount, MaxOutstandingByteCount);
        }

        return null;
    }

    internal void Validate()
    {
        if (string.IsNullOrWhiteSpace(Sufix))
            throw new ArgumentException($"{nameof(Sufix)} must be provided if {nameof(Subscription)} is configured.");

        if (MessageRetentionDurationDays < MinimumAcceptableMessageRetentionDurationDays)
            throw new ArgumentException($"{nameof(MessageRetentionDurationDays)} must be provided if {nameof(Subscription)} is configured and must be greater or equal than {MinimumAcceptableMessageRetentionDurationDays} days.");

        if (AckDeadlineSeconds < MinimumAcceptableAckDeadlineSeconds)
            throw new ArgumentException($"{nameof(AckDeadlineSeconds)} must be provided if {nameof(Subscription)} is configured and must be greater or equal than {MinimumAcceptableAckDeadlineSeconds} seconds.");

        if (MaxDeliveryAttempts < MinimumAcceptableMaxDeliveryAttempts)
            throw new ArgumentException($"{nameof(MaxDeliveryAttempts)} must be provided if {nameof(Subscription)} is configured and must be greater or equal than {MinimumAcceptableMaxDeliveryAttempts}.");

        if (MinBackoffSeconds < MinimumAcceptableMinBackoffSeconds)
            throw new ArgumentException($"{nameof(MinBackoffSeconds)} must be provided if {nameof(Subscription)} is configured and must be greater or equal than {MinimumAcceptableMinBackoffSeconds} seconds.");

        if (MaxBackoffSeconds < MinimumAcceptableMaxBackoffSeconds)
            throw new ArgumentException($"{nameof(MaxBackoffSeconds)} must be provided if {nameof(Subscription)} is configured and must be greater or equal than {MinimumAcceptableMaxBackoffSeconds} seconds.");
    }
}

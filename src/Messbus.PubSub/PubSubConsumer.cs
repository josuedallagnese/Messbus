using Google.Cloud.PubSub.V1;
using Messbus.PubSub.Internal;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Messbus.PubSub;

public class PubSubConsumer<TEvent, TConsumer> : MessageConsumer<TEvent, TConsumer>
    where TConsumer : class, IMessageConsumer<TEvent>
{
    private readonly SubscriptionId _subscriptionId;
    private readonly EnvironmentId _environmentId;
    private readonly bool _isDeadLetterConsumer;
    private readonly IHostApplicationLifetime _lifetime;

    private SubscriberClient _subscriberClient;

    public PubSubConsumer(
        SubscriptionId subscriptionId,
        EnvironmentId environmentId,
        bool isDeadLetterConsumer,
        IServiceProvider serviceProvider,
        IMessageSerializer messageSerializer,
        IHostApplicationLifetime lifetime)
        : base(serviceProvider, messageSerializer, environmentId.VerbosityMode)
    {
        _subscriptionId = subscriptionId;
        _environmentId = environmentId;
        _isDeadLetterConsumer = isDeadLetterConsumer;
        _lifetime = lifetime;
    }

    protected override async Task InitializeProcessing(CancellationToken stoppingToken)
    {
        if (_environmentId.Unmanaged)
        {
            _environmentId.Initialized();
            return;
        }

        var deadLetterSubscription = _subscriptionId.DeadLetterSubscription;

        if (_isDeadLetterConsumer)
        {
            await _environmentId.EnsureSubscriptionExists(
                deadLetterSubscription.SubscriptionName,
                Logger,
                maxAttempts: 10,
                delayInSeconds: 10,
                _lifetime,
                stoppingToken);

            return;
        }

        var topic = _subscriptionId.TopicId.Topic;
        var deadLetterTopic = _subscriptionId.DeadLetterTopic;
        var subscription = _subscriptionId.Subscription;

        await _environmentId.CreateTopic(topic, Logger, stoppingToken);
        await _environmentId.CreateTopic(deadLetterTopic, Logger, stoppingToken);
        await _environmentId.CreateSubscription(deadLetterSubscription, Logger, stoppingToken);
        await _environmentId.CreateSubscription(subscription, Logger, stoppingToken);
        await _environmentId.AddRoleBindings(_subscriptionId, Logger, stoppingToken);

        _environmentId.Initialized();
    }

    protected override async Task StartProcessing(CancellationToken stoppingToken)
    {
        var subscriptionName = _isDeadLetterConsumer
            ? _subscriptionId.DeadLetterSubscription.SubscriptionName
            : _subscriptionId.Subscription.SubscriptionName;

        _subscriberClient = _environmentId.GetSubscriberClient(subscriptionName);

        await _subscriberClient.StartAsync(ConsumerHandler);
    }

    protected override async Task StopProcessing(CancellationToken stoppingToken)
    {
        if (_subscriberClient is not null)
        {
            await _subscriberClient.StopAsync(stoppingToken);
            await _subscriberClient.DisposeAsync();
            _subscriberClient = null;
        }
    }

    private async Task<SubscriberClient.Reply> ConsumerHandler(PubsubMessage pubsubMessage, CancellationToken stoppingToken)
    {
        var attempt = pubsubMessage.GetDeliveryAttempt() ?? 0;

        try
        {
            var context = new MessageContext<TEvent>(
                pubsubMessage.MessageId,
                attempt,
                pubsubMessage.Data.ToByteArray());

            await ProcessMessage(context, stoppingToken);

            return SubscriberClient.Reply.Ack;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Message=Error handling message; MessageId={Id}, Attempt={Attempt}, Consumer={Consumer}",
                pubsubMessage.MessageId,
                attempt,
                ConsumerName);

            return SubscriberClient.Reply.Nack;
        }
    }
}

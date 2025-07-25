using Google.Cloud.PubSub.V1;
using Grpc.Core;
using MessageBus.PubSub.Configuration;
using MessageBus.PubSub.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MessageBus.PubSub;

public class PubSubConsumer<TEvent, TConsumer> : MessageConsumer<TEvent, TConsumer>
    where TConsumer : class, IMessageConsumer<TEvent>
{
    private readonly ChannelCredentials _channelCredentials;
    private readonly SubscriptionId _subscriptionId;
    private readonly bool _isDeadLetter;

    private SubscriberClient _subscriberClient;

    public PubSubConsumer(
        ChannelCredentials channelCredentials,
        SubscriptionId subscriptionId,
        bool isDeadLetter,
        IServiceScopeFactory serviceScopeFactory,
        IPubSubSerializer serializer,
        ILogger<MessageConsumer<TEvent, TConsumer>> logger)
        : base(serviceScopeFactory, serializer, logger)
    {
        _channelCredentials = channelCredentials;
        _subscriptionId = subscriptionId;
        _isDeadLetter = isDeadLetter;
    }

    protected override async Task InitializeProcessing(CancellationToken stoppingToken)
    {
        if (!_subscriptionId.PubSubConfiguration.ShouldInitializeSubscriptions())
            return;

        var publisherService = await new PublisherServiceApiClientBuilder
        {
            ChannelCredentials = _channelCredentials,
            EmulatorDetection = _subscriptionId.PubSubConfiguration.EmulatorDetection
        }.BuildAsync(stoppingToken);

        var subscriberService = await new SubscriberServiceApiClientBuilder
        {
            ChannelCredentials = _channelCredentials,
            EmulatorDetection = _subscriptionId.PubSubConfiguration.EmulatorDetection
        }.BuildAsync(stoppingToken);

        await InitializeConsumerTopic(publisherService, stoppingToken);

        await InitializeConsumerDeadLetterTopic(publisherService, stoppingToken);

        await InitializeConsumerDeadLetterSubscription(subscriberService, stoppingToken);

        await InitializeConsumerSubscription(subscriberService, stoppingToken);
    }

    protected override async Task StartProcessing(CancellationToken stoppingToken)
    {
        _subscriberClient = await new SubscriberClientBuilder()
        {
            ChannelCredentials = _channelCredentials,
            SubscriptionName = _isDeadLetter ? _subscriptionId.DeadLetterSubscriptionName : _subscriptionId.SubscriptionName,
            EmulatorDetection = _subscriptionId.PubSubConfiguration.EmulatorDetection
        }.BuildAsync(stoppingToken);

        await _subscriberClient.StartAsync(ConsumerHandler);
    }

    protected override async Task StopProcessing(CancellationToken stoppingToken)
    {
        if (_subscriberClient is not null)
        {
            await _subscriberClient.StopAsync(stoppingToken);
            await _subscriberClient.DisposeAsync();
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

    private async Task InitializeConsumerTopic(PublisherServiceApiClient publisherService, CancellationToken stoppingToken)
    {
        var topic = _subscriptionId.TopicId.GetTopic();

        try
        {
            await publisherService.CreateTopicAsync(
                topic,
                stoppingToken);

            Logger.LogInformation("Message=Topic {TopicName} was created; Subscription={Subscription}; ProjectId={ProjectId};",
                topic.Name,
                _subscriptionId.SubscriptionName.SubscriptionId,
                _subscriptionId.SubscriptionName.ProjectId);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.AlreadyExists)
        {
            Logger.LogInformation("Message=Topic {TopicName} already exists; Subscription={Subscription}; ProjectId={ProjectId};",
                topic.Name,
                _subscriptionId.SubscriptionName.SubscriptionId,
                _subscriptionId.SubscriptionName.ProjectId);
        }
    }

    private async Task InitializeConsumerDeadLetterTopic(PublisherServiceApiClient publisherService, CancellationToken stoppingToken)
    {
        var topic = _subscriptionId.GetDeadLetterTopic();

        try
        {
            await publisherService.CreateTopicAsync(
                topic,
                stoppingToken);

            Logger.LogInformation("Message=Dead lettering topic {TopicName} was created; Subscription={Subscription}; ProjectId={ProjectId};",
                topic.Name,
                _subscriptionId.SubscriptionName.SubscriptionId,
                _subscriptionId.SubscriptionName.ProjectId);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.AlreadyExists)
        {
            Logger.LogInformation("Message=Dead lettering topic {TopicName} already exists; Subscription={Subscription}; ProjectId={ProjectId};",
                topic.Name,
                _subscriptionId.SubscriptionName.SubscriptionId,
                _subscriptionId.SubscriptionName.ProjectId);
        }
    }

    private async Task InitializeConsumerDeadLetterSubscription(SubscriberServiceApiClient subscriberService, CancellationToken stoppingToken)
    {
        var deadLetterSubscription = _subscriptionId.GetDeadLetterSubscription();

        try
        {
            await subscriberService.CreateSubscriptionAsync(deadLetterSubscription, stoppingToken);

            Logger.LogInformation("Message=Dead letter subscription {TopicName} was created; Subscription={Subscription}; ProjectId={ProjectId};",
                deadLetterSubscription.Topic,
                _subscriptionId.SubscriptionName.SubscriptionId,
                _subscriptionId.SubscriptionName.ProjectId);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.AlreadyExists)
        {
            Logger.LogInformation("Message=Dead letter subscription {TopicName} already exists; Subscription={Subscription}; ProjectId={ProjectId};",
                deadLetterSubscription.Topic,
                _subscriptionId.SubscriptionName.SubscriptionId,
                _subscriptionId.SubscriptionName.ProjectId);
        }
    }

    private async Task InitializeConsumerSubscription(SubscriberServiceApiClient subscriberService, CancellationToken stoppingToken)
    {
        var subscription = _subscriptionId.GetSubscription();

        try
        {
            await subscriberService.CreateSubscriptionAsync(subscription, stoppingToken);

            Logger.LogInformation("Message=Subscription {TopicName} was created; Subscription={Subscription}; ProjectId={ProjectId};",
                subscription.Name,
                _subscriptionId.SubscriptionName.SubscriptionId,
                _subscriptionId.SubscriptionName.ProjectId);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.AlreadyExists)
        {
            Logger.LogInformation("Message=Subscription {TopicName} already exists; Subscription={Subscription}; ProjectId={ProjectId};",
                subscription.Name,
                _subscriptionId.SubscriptionName.SubscriptionId,
                _subscriptionId.SubscriptionName.ProjectId);
        }
    }
}

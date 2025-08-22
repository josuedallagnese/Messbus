using Google.Cloud.PubSub.V1;
using Grpc.Core;
using MessageBus.PubSub.Configuration;
using MessageBus.PubSub.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MessageBus.PubSub;

public class PubSubMessageBus : MessageBus
{
    private readonly PubSubConfiguration _pubSubConfiguration;
    private readonly ChannelCredentials _channelCredentials;
    private readonly Dictionary<string, PublisherClient> _publishers;
    private readonly PublisherServiceApiClient _publisherServiceApiClient;

    public PubSubMessageBus(
        PubSubConfiguration pubSubConfiguration,
        IServiceScopeFactory serviceScopeFactory,
        IPubSubSerializer serializer,
        ILogger<MessageBus> logger,
        bool verbosityMode)
        : base(serviceScopeFactory, serializer, logger, verbosityMode)
    {
        _pubSubConfiguration = pubSubConfiguration;
        _channelCredentials = pubSubConfiguration.GetChannelCredentials();

        var publisherBuilder = new PublisherServiceApiClientBuilder();

        if (_pubSubConfiguration.UseEmulator)
            publisherBuilder.EmulatorDetection = Google.Api.Gax.EmulatorDetection.EmulatorOnly;
        else
            publisherBuilder.ChannelCredentials = _channelCredentials;


        _publisherServiceApiClient = publisherBuilder.Build();

        _publishers = BuildPublishers();
    }

    protected override async Task<string> Send(string topic, byte[] message, CancellationToken cancellationToken = default)
    {
        if (!_publishers.TryGetValue(topic, out var publisher))
            throw new InvalidOperationException($"Publisher not found for topic '{topic}'");

        string messageId = default;

        try
        {
            messageId = await publisher.PublishAsync(message);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            Logger.LogError("Message=Topic {TopicName} not found; ProjectId={ProjectId}",
                topic,
                _pubSubConfiguration.ProjectId);

            if (_pubSubConfiguration.ShouldInitializeTopics())
            {
                await CreateTopic(topic, cancellationToken);

                messageId = await publisher.PublishAsync(message);
            }
        }

        return messageId;
    }

    private Dictionary<string, PublisherClient> BuildPublishers()
    {
        var result = new Dictionary<string, PublisherClient>();

        foreach (var topic in _pubSubConfiguration.Publishing.Topics)
        {
            var topicId = _pubSubConfiguration.GetTopicId(topic);

            var builder = new PublisherClientBuilder
            {
                TopicName = topicId.TopicName
            };

            if (_pubSubConfiguration.UseEmulator)
                builder.EmulatorDetection = Google.Api.Gax.EmulatorDetection.EmulatorOnly;
            else
                builder.ChannelCredentials = _channelCredentials;

            result[topic] = builder.Build();
        }

        return result;
    }

    private async Task CreateTopic(string topic, CancellationToken stoppingToken)
    {
        try
        {
            var topicId = _pubSubConfiguration.GetTopicId(topic);

            var topicRequest = topicId.GetTopic();

            await _publisherServiceApiClient.CreateTopicAsync(
                topicRequest,
                stoppingToken);

            Logger.LogInformation("Message=Topic {TopicName} was created; ProjectId={ProjectId}",
                topic,
                _pubSubConfiguration.ProjectId);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.AlreadyExists)
        {
            Logger.LogInformation("Message=Topic {TopicName} already exists; ProjectId={ProjectId}",
                topic,
                _pubSubConfiguration.ProjectId);
        }
    }
}

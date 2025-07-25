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
        ILogger<MessageBus> logger)
        : base(serviceScopeFactory, serializer, logger)
    {
        _pubSubConfiguration = pubSubConfiguration;
        _channelCredentials = pubSubConfiguration.GetChannelCredentials();

        _publisherServiceApiClient = (new PublisherServiceApiClientBuilder()
        {
            ChannelCredentials = _channelCredentials,
            EmulatorDetection = pubSubConfiguration.EmulatorDetection
        }).Build();

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
                TopicName = topicId.TopicName,
                ChannelCredentials = _channelCredentials,
                EmulatorDetection = _pubSubConfiguration.EmulatorDetection
            };

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

using Google.Cloud.PubSub.V1;
using Google.Protobuf;
using Grpc.Core;
using Messbus.PubSub.Configuration;

namespace Messbus.PubSub;

public class PubSubMessageBus : MessageBus
{
    private readonly PubSubConfiguration _pubSubConfiguration;

    public PubSubMessageBus(
        PubSubConfiguration pubSubConfiguration,
        IServiceProvider serviceProvider,
        IMessageSerializer serializer)
        : base(serviceProvider, serializer)
    {
        _pubSubConfiguration = pubSubConfiguration;
    }

    protected override async Task<IEnumerable<string>> Send(string topic, IEnumerable<byte[]> messages, CancellationToken cancellationToken = default)
    {
        var messagesToSend = messages.Select(m => new PubsubMessage()
        {
            Data = ByteString.CopyFrom(m)
        }).ToArray();

        var environmentId = _pubSubConfiguration.GetEnvironmentId(topic);

        var topicId = _pubSubConfiguration.GetTopicId(topic);

        try
        {
            var messageIds = await environmentId.Publish(topicId.TopicName, messagesToSend, Logger, cancellationToken);

            return messageIds;
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            if (_pubSubConfiguration.ShouldInitializeTopics())
            {
                await environmentId.CreateTopic(topicId.Topic, Logger, cancellationToken);

                var messageIds = await environmentId.Publish(topicId.TopicName, messagesToSend, Logger, cancellationToken);

                return messageIds;
            }

            throw;
        }
    }
}

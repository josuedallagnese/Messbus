using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MessageBus;

public abstract class MessageBus : IMessageBus
{
    protected IServiceScopeFactory ServiceScopeFactory;
    protected IMessageSerializer Serializer;
    protected ILogger<MessageBus> Logger;

    protected MessageBus(
        IServiceScopeFactory serviceScopeFactory,
        IMessageSerializer serializer,
        ILogger<MessageBus> logger)
    {
        ServiceScopeFactory = serviceScopeFactory;
        Serializer = serializer;
        Logger = logger;
    }

    public async Task<string> Publish<T>(string topic, T message, CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Sending message to topic. Message={Message}; Topic={Topic};",
           topic,
           message);

        var data = Serializer.Serialize(message);

        using var scope = ServiceScopeFactory.CreateScope();

        var messageId = await Send(topic, data, cancellationToken);

        Logger.LogInformation("Message is sent. MessageId={MessageId}; Topic={Topic}", messageId, topic);

        return messageId;
    }

    protected abstract Task<string> Send(string topic, byte[] message, CancellationToken cancellationToken = default);
}

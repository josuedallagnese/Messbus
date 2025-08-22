using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MessageBus;

public abstract class MessageBus : IMessageBus
{
    protected IServiceScopeFactory ServiceScopeFactory;
    protected IMessageSerializer Serializer;
    protected ILogger Logger;
    protected bool VerbosityMode;

    protected MessageBus(
        IServiceScopeFactory serviceScopeFactory,
        IMessageSerializer serializer,
        ILogger<MessageBus> logger,
        bool verbosityMode)
    {
        ServiceScopeFactory = serviceScopeFactory;
        Serializer = serializer;
        Logger = logger;
        VerbosityMode = verbosityMode;
    }

    public async Task<string> Publish<T>(string topic, T message, CancellationToken cancellationToken = default)
    {
        using var scope = ServiceScopeFactory.CreateScope();

        if (VerbosityMode)
            Logger.LogInformation("Sending message to topic. Message={@Message}; Topic={Topic};", topic, message);

        var data = Serializer.Serialize(message);

        var messageId = await Send(topic, data, cancellationToken);

        Logger.LogInformation("Message sent successfully. MessageId={MessageId}; Topic={Topic}; DataLength={DataLength}", messageId, topic, data.Length);

        return messageId;
    }

    protected abstract Task<string> Send(string topic, byte[] message, CancellationToken cancellationToken = default);
}

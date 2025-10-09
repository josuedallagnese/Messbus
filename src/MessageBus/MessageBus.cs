using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MessageBus;

public abstract class MessageBus : IMessageBus
{
    protected IServiceProvider ServiceProvider;
    protected IMessageSerializer Serializer;
    protected ILogger Logger;

    protected MessageBus(
        IServiceProvider serviceProvider,
        IMessageSerializer serializer)
    {
        ServiceProvider = serviceProvider;
        Serializer = serializer;
        Logger = serviceProvider.GetRequiredService<ILogger<MessageBus>>();
    }

    public async Task<string> Publish<T>(string topic, T message, CancellationToken cancellationToken = default)
        where T : class
    {
        var messageIds = await PublishBatch(topic, new[] { message }, cancellationToken);

        return messageIds.FirstOrDefault();
    }

    public async Task<IEnumerable<string>> PublishBatch<T>(string topic, IEnumerable<T> messages, CancellationToken cancellationToken = default)
        where T : class
    {
        using var scope = ServiceProvider.CreateScope();

        var data = messages.Select(s => Serializer.Serialize(s)).ToArray();
        var total = data.Length;

        return await Send(topic, data, cancellationToken);
    }

    protected abstract Task<IEnumerable<string>> Send(string topic, IEnumerable<byte[]> messages, CancellationToken cancellationToken = default);
}

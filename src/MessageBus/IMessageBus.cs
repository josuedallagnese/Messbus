namespace MessageBus;

public interface IMessageBus
{
    Task<string> Publish<T>(string topic, T message, CancellationToken cancellationToken = default);
}

namespace Messbus;

public interface IMessageBus
{
    Task<string> Publish<T>(string topic, T message, CancellationToken cancellationToken = default)
        where T : class;

    Task<IEnumerable<string>> PublishBatch<T>(string topic, IEnumerable<T> messages, CancellationToken cancellationToken = default)
        where T : class;
}

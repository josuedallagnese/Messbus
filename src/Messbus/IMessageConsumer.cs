namespace Messbus;

public interface IMessageConsumer<TEvent>
{
    Task Handler(MessageContext<TEvent> context, CancellationToken cancellationToken = default);
}

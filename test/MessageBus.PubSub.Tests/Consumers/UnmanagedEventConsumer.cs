using MessageBus.PubSub.Tests.Events;

namespace MessageBus.PubSub.Tests.Consumers;

public class UnmanagedEventConsumer : IMessageConsumer<UnmanagedEvent>
{
    private readonly ConsumerCollector<UnmanagedEvent, UnmanagedEventConsumer> _consumerCollector;

    public UnmanagedEventConsumer(ConsumerCollector<UnmanagedEvent, UnmanagedEventConsumer> consumerCollector)
    {
        _consumerCollector = consumerCollector;
    }

    public async Task Handler(MessageContext<UnmanagedEvent> message, CancellationToken cancellationToken = default)
    {
        _consumerCollector.AddAttempt();

        await _consumerCollector.Successful(message.Message);
    }
}

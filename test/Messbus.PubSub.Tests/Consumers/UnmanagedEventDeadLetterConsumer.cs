using Messbus.PubSub.Tests.Events;

namespace Messbus.PubSub.Tests.Consumers;

public class UnmanagedEventDeadLetterConsumer : IMessageConsumer<UnmanagedEvent>
{
    private readonly ConsumerCollector<UnmanagedEvent, UnmanagedEventConsumer> _consumerCollector;

    public UnmanagedEventDeadLetterConsumer(ConsumerCollector<UnmanagedEvent, UnmanagedEventConsumer> consumerCollector)
    {
        _consumerCollector = consumerCollector;
    }

    public async Task Handler(MessageContext<UnmanagedEvent> message, CancellationToken cancellationToken = default)
    {
        await _consumerCollector.Unsuccessful();
    }
}

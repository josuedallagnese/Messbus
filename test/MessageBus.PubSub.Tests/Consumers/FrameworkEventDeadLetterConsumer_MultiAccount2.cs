using MessageBus.PubSub.Tests.Events;

namespace MessageBus.PubSub.Tests.Consumers;

public class FrameworkEventDeadLetterConsumer_MultiAccount2 : IMessageConsumer<FrameworkEvent>
{
    private readonly ConsumerCollector<FrameworkEvent, FrameworkEventConsumer_MultiAccount2> _consumerCollector;

    public FrameworkEventDeadLetterConsumer_MultiAccount2(ConsumerCollector<FrameworkEvent, FrameworkEventConsumer_MultiAccount2> consumerCollector)
    {
        _consumerCollector = consumerCollector;
    }

    public async Task Handler(MessageContext<FrameworkEvent> context, CancellationToken cancellationToken = default)
    {
        await _consumerCollector.Unsuccessful();
    }
}

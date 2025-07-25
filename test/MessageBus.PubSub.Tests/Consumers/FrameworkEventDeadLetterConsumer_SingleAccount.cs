using MessageBus.PubSub.Tests.Events;

namespace MessageBus.PubSub.Tests.Consumers;

public class FrameworkEventDeadLetterConsumer_SingleAccount : IMessageConsumer<FrameworkEvent>
{
    private readonly ConsumerCollector<FrameworkEvent, FrameworkEventConsumer_SingleAccount> _consumerCollector;

    public FrameworkEventDeadLetterConsumer_SingleAccount(ConsumerCollector<FrameworkEvent, FrameworkEventConsumer_SingleAccount> consumerCollector)
    {
        _consumerCollector = consumerCollector;
    }

    public async Task Handler(MessageContext<FrameworkEvent> message, CancellationToken cancellationToken = default)
    {
        await _consumerCollector.Unsuccessful();
    }
}

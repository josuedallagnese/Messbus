using Messbus.PubSub.Tests.Events;

namespace Messbus.PubSub.Tests.Consumers;

public class FrameworkEventConsumer_SingleAccount : IMessageConsumer<FrameworkEvent>
{
    private readonly ConsumerCollector<FrameworkEvent, FrameworkEventConsumer_SingleAccount> _consumerCollector;

    public FrameworkEventConsumer_SingleAccount(ConsumerCollector<FrameworkEvent, FrameworkEventConsumer_SingleAccount> consumerCollector)
    {
        _consumerCollector = consumerCollector;
    }

    public async Task Handler(MessageContext<FrameworkEvent> message, CancellationToken cancellationToken = default)
    {
        _consumerCollector.AddAttempt();

        await _consumerCollector.Successful(message.Message);
    }
}

using Messbus.PubSub.Tests.Events;

namespace Messbus.PubSub.Tests.Consumers;

public class FrameworkEventConsumer_MultiAccount1 : IMessageConsumer<FrameworkEvent>
{
    private readonly ConsumerCollector<FrameworkEvent, FrameworkEventConsumer_MultiAccount1> _consumerCollector;

    public FrameworkEventConsumer_MultiAccount1(ConsumerCollector<FrameworkEvent, FrameworkEventConsumer_MultiAccount1> consumerCollector)
    {
        _consumerCollector = consumerCollector;
    }

    public async Task Handler(MessageContext<FrameworkEvent> context, CancellationToken cancellationToken = default)
    {
        _consumerCollector.AddAttempt();

        await _consumerCollector.Successful(context.Message);
    }
}

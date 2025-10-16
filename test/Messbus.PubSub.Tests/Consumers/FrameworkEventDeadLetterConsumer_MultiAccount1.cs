﻿using Messbus.PubSub.Tests.Events;

namespace Messbus.PubSub.Tests.Consumers;

public class FrameworkEventDeadLetterConsumer_MultiAccount1 : IMessageConsumer<FrameworkEvent>
{
    private readonly ConsumerCollector<FrameworkEvent, FrameworkEventConsumer_MultiAccount1> _consumerCollector;

    public FrameworkEventDeadLetterConsumer_MultiAccount1(ConsumerCollector<FrameworkEvent, FrameworkEventConsumer_MultiAccount1> consumerCollector)
    {
        _consumerCollector = consumerCollector;
    }

    public async Task Handler(MessageContext<FrameworkEvent> context, CancellationToken cancellationToken = default)
    {
        await _consumerCollector.Unsuccessful();
    }
}

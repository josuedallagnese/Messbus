using Messbus.PubSub.Tests.Events;
using Microsoft.Extensions.Logging;

namespace Messbus.PubSub.Tests.Consumers;

public class LoadEventConsumer : IMessageConsumer<LoadEvent>
{

    private readonly ConsumerCollector<LoadEvent, LoadEventConsumer> _consumerCollector;
    private readonly ILogger<LoadEventConsumer> _logger;

    public LoadEventConsumer(ConsumerCollector<LoadEvent, LoadEventConsumer> consumerCollector, ILogger<LoadEventConsumer> logger)
    {
        _consumerCollector = consumerCollector;
        _logger = logger;
    }

    public async Task Handler(MessageContext<LoadEvent> context, CancellationToken cancellationToken = default)
    {
        await _consumerCollector.AddMessageCount();

        _logger.LogInformation($"Current message id: {context.Message.Id}");
        _logger.LogInformation($"Current message count: {_consumerCollector.MessageCount}");
    }
}

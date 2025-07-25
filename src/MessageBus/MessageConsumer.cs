using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MessageBus;

public abstract class MessageConsumer<TEvent, TConsumer> : BackgroundService
    where TConsumer : IMessageConsumer<TEvent>
{
    protected IServiceScopeFactory ServiceScopeFactory;
    protected IMessageSerializer Serializer;
    protected ILogger<MessageConsumer<TEvent, TConsumer>> Logger;
    protected string ConsumerName;

    protected MessageConsumer(
        IServiceScopeFactory serviceScopeFactory,
        IMessageSerializer serializer,
        ILogger<MessageConsumer<TEvent, TConsumer>> logger)
    {
        ServiceScopeFactory = serviceScopeFactory;
        Serializer = serializer;
        Logger = logger;

        ConsumerName = typeof(TConsumer).Name;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Logger.LogInformation("Initializing consumer. Consumer={Consumer}", ConsumerName);

        await InitializeProcessing(stoppingToken);

        Logger.LogInformation("Starting consumer. Consumer={Consumer}", ConsumerName);

        await StartProcessing(stoppingToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("Stopping consumer. Consumer={Consumer}", ConsumerName);

        await StopProcessing(cancellationToken);

        await base.StopAsync(cancellationToken);
    }

    protected abstract Task InitializeProcessing(CancellationToken stoppingToken);

    protected abstract Task StartProcessing(CancellationToken stoppingToken);

    protected abstract Task StopProcessing(CancellationToken stoppingToken);

    protected virtual async Task ProcessMessage(MessageContext<TEvent> context, CancellationToken cancellationToken)
    {
        var message = Serializer.Deserialize<TEvent>(context.Data);

        context.SetMessage(message);

        Logger.LogInformation("Deserialized message successfully. MessageId={Id}, Attempt={Attempt}, Consumer={Consumer}", context.Id, context.Attempt, ConsumerName);

        using var scope = ServiceScopeFactory.CreateScope();

        var consumer = scope.ServiceProvider.GetRequiredService<TConsumer>();

        await consumer.Handler(context, cancellationToken);

        Logger.LogInformation("Message handled successfully. MessageId={Id}, Attempt={Attempt}, Consumer={Consumer}", context.Id, context.Attempt, ConsumerName);
    }
}

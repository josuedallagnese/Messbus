using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Messbus;

public abstract class MessageConsumer<TEvent, TConsumer> : BackgroundService
    where TConsumer : IMessageConsumer<TEvent>
{
    protected IServiceProvider ServiceProvider;
    protected IMessageSerializer Serializer;
    protected ILogger Logger;
    protected bool VerbosityMode;
    protected string ConsumerName;

    protected MessageConsumer(
        IServiceProvider serviceProvider,
        IMessageSerializer serializer,
        bool verbosityMode)
    {
        ServiceProvider = serviceProvider;
        Serializer = serializer;
        Logger = serviceProvider.GetService<ILogger<MessageConsumer<TEvent, TConsumer>>>();
        VerbosityMode = verbosityMode;

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
        var data = Serializer.Deserialize<TEvent>(context.Data);

        context.SetMessage(data);

        if (VerbosityMode)
            Logger.LogInformation("Deserialized message successfully. MessageId={Id}, Attempt={Attempt}, Consumer={Consumer}, Message={@Message}", context.Id, context.Attempt, ConsumerName, context.Message);

        using var scope = ServiceProvider.CreateScope();

        var consumer = scope.ServiceProvider.GetRequiredService<TConsumer>();

        await consumer.Handler(context, cancellationToken);

        Logger.LogInformation("Message handled successfully. MessageId={Id}, Attempt={Attempt}, Consumer={Consumer}", context.Id, context.Attempt, ConsumerName);
    }
}

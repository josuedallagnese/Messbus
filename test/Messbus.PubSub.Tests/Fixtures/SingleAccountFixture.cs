using Messbus.PubSub.Configuration;
using Messbus.PubSub.Internal;
using Messbus.PubSub.Tests.Consumers;
using Messbus.PubSub.Tests.Events;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;

namespace Messbus.PubSub.Tests.Fixtures;

public class SingleAccountFixture : AccountFixture
{
    public IServiceProvider Provider { get; private set; }
    public EnvironmentId EnvironmentId { get; private set; }
    public SubscriptionId SubscriptionId { get; private set; }
    public ILogger Logger { get; private set; }

    public override async Task Initialize()
    {
        var services = new ServiceCollection()
            .AddSingleton(new Mock<IHostApplicationLifetime>().Object);

        services.AddLogging(c =>
        {
            c.ClearProviders();
            c.AddConsole();
        });

        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.SingleAccount.json", false)
            .Build();

        var pubSubConfiguration = new PubSubConfiguration(configuration);

        services.AddPubSub(pubSubConfiguration)
            .AddConsumer<FrameworkEvent, FrameworkEventConsumer_SingleAccount, FrameworkEventDeadLetterConsumer_SingleAccount>("framework");

        services.AddSingleton<ConsumerCollector<FrameworkEvent, FrameworkEventConsumer_SingleAccount>>();

        Provider = services.BuildServiceProvider();
        Logger = Provider.GetRequiredService<ILogger<SingleAccountFixture>>();

        EnvironmentId = pubSubConfiguration.GetEnvironmentId("framework");
        SubscriptionId = pubSubConfiguration.GetSubscriptionId("framework");

        await Cleanup(EnvironmentId, SubscriptionId, Logger);
    }
}

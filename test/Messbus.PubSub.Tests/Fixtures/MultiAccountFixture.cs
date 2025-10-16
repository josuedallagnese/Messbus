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

public class MultiAccountFixture : AccountFixture
{
    public IServiceProvider Provider { get; private set; }

    public EnvironmentId EnvironmentIdAccount1 { get; private set; }
    public SubscriptionId SubscriptionIdAccount1 { get; private set; }
    public EnvironmentId EnvironmentIdAccount2 { get; private set; }
    public SubscriptionId SubscriptionIdAccount2 { get; private set; }
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
            .AddJsonFile("appsettings.MultiAccount.json", false)
            .Build();

        var pubSubConfigurationAccount1 = new PubSubConfiguration("Account1", configuration);

        services.AddPubSub(pubSubConfigurationAccount1)
            .AddConsumer<FrameworkEvent, FrameworkEventConsumer_MultiAccount1, FrameworkEventDeadLetterConsumer_MultiAccount1>("framework");

        services.AddSingleton<ConsumerCollector<FrameworkEvent, FrameworkEventConsumer_MultiAccount1>>();

        var pubSubConfigurationAccount2 = new PubSubConfiguration("Account2", configuration);

        services.AddPubSub(pubSubConfigurationAccount2)
            .AddConsumer<FrameworkEvent, FrameworkEventConsumer_MultiAccount2, FrameworkEventDeadLetterConsumer_MultiAccount2>("framework");

        services.AddSingleton<ConsumerCollector<FrameworkEvent, FrameworkEventConsumer_MultiAccount2>>();

        Provider = services.BuildServiceProvider();
        Logger = Provider.GetRequiredService<ILogger<MultiAccountFixture>>();

        EnvironmentIdAccount1 = pubSubConfigurationAccount1.GetEnvironmentId("framework");
        SubscriptionIdAccount1 = pubSubConfigurationAccount1.GetSubscriptionId("framework");

        EnvironmentIdAccount2 = pubSubConfigurationAccount2.GetEnvironmentId("framework");
        SubscriptionIdAccount2 = pubSubConfigurationAccount2.GetSubscriptionId("framework");

        await Cleanup(EnvironmentIdAccount1, SubscriptionIdAccount1, Logger);
        await Cleanup(EnvironmentIdAccount2, SubscriptionIdAccount2, Logger);
    }
}

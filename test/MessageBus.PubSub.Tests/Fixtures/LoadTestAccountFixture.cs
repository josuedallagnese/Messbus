using MessageBus.PubSub.Configuration;
using MessageBus.PubSub.Internal;
using MessageBus.PubSub.Tests.Consumers;
using MessageBus.PubSub.Tests.Events;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;

namespace MessageBus.PubSub.Tests.Fixtures;

public class LoadTestAccountFixture : AccountFixture
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
            .AddJsonFile("appsettings.LoadTest.json", false)
            .Build();

        var pubSubConfiguration = new PubSubConfiguration(configuration);

        services.AddPubSub(pubSubConfiguration)
            .AddUnmanagedConsumer<LoadEvent, LoadEventConsumer>("load-test", "load-test.message-bus");

        services.AddSingleton<ConsumerCollector<LoadEvent, LoadEventConsumer>>();

        Provider = services.BuildServiceProvider();
        Logger = Provider.GetRequiredService<ILogger<UnmanagedAccountFixture>>();

        EnvironmentId = pubSubConfiguration.GetEnvironmentId("load-test", "load-test.message-bus");
        SubscriptionId = pubSubConfiguration.GetSubscriptionId("load-test", "load-test.message-bus");

        await InitializeUnmanaged(EnvironmentId, SubscriptionId, Logger);
    }
}

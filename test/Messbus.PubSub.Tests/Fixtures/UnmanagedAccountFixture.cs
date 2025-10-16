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

public class UnmanagedAccountFixture : AccountFixture
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
            .AddJsonFile("appsettings.UnmanagedAccount.json", false)
            .Build();

        var pubSubConfiguration = new PubSubConfiguration(configuration);

        services.AddPubSub(pubSubConfiguration)
            .AddUnmanagedConsumer<UnmanagedEvent, UnmanagedEventConsumer, UnmanagedEventDeadLetterConsumer>("unmanaged", "unmanaged.message-bus");

        services.AddSingleton<ConsumerCollector<UnmanagedEvent, UnmanagedEventConsumer>>();

        Provider = services.BuildServiceProvider();
        Logger = Provider.GetRequiredService<ILogger<UnmanagedAccountFixture>>();

        EnvironmentId = pubSubConfiguration.GetEnvironmentId("unmanaged", "unmanaged.message-bus");
        SubscriptionId = pubSubConfiguration.GetSubscriptionId("unmanaged", "unmanaged.message-bus");

        await InitializeUnmanaged(EnvironmentId, SubscriptionId, Logger);
    }
}

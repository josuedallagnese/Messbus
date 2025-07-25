using MessageBus.PubSub.Tests.Consumers;
using MessageBus.PubSub.Tests.Events;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MessageBus.PubSub.Tests.Fixtures;

public class MultiAccountFixture : IDisposable
{
    public IHost Host { get; }

    public MultiAccountFixture()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.MultiAccount.json", false)
            .Build();

        Host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
            .ConfigureHostConfiguration(builder =>
            {
                builder.AddConfiguration(configuration);
            })
            .ConfigureServices((context, services) =>
            {
                services.AddPubSub(configuration, "Account1")
                    .AddConsumer<FrameworkEvent, FrameworkEventConsumer_MultiAccount1, FrameworkEventDeadLetterConsumer_MultiAccount1>("framework");

                services.AddSingleton<ConsumerCollector<FrameworkEvent, FrameworkEventConsumer_MultiAccount1>>();

                services.AddPubSub(configuration, "Account2")
                    .AddConsumer<FrameworkEvent, FrameworkEventConsumer_MultiAccount2, FrameworkEventDeadLetterConsumer_MultiAccount2>("framework");

                services.AddSingleton<ConsumerCollector<FrameworkEvent, FrameworkEventConsumer_MultiAccount2>>();
            }).Build();

        Host.Start();
    }

    public IMessageBus GetPublisher(string alias)
    {
        return Host.Services.GetRequiredKeyedService<IMessageBus>(alias);
    }

    public ConsumerCollector<TEvent, TConsumer> GetConsumerCollector<TEvent, TConsumer>()
        where TConsumer : IMessageConsumer<TEvent>
    {
        return Host.Services.GetRequiredService<ConsumerCollector<TEvent, TConsumer>>();
    }

    public void Dispose()
    {
        Host.StopAsync().GetAwaiter().GetResult();
        Host.Dispose();
    }
}

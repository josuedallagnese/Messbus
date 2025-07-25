using MessageBus.PubSub.Tests.Consumers;
using MessageBus.PubSub.Tests.Events;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MessageBus.PubSub.Tests.Fixtures;

public class SingleAccountFixture : IDisposable
{
    public IHost Host { get; }

    public SingleAccountFixture()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.SingleAccount.json", false)
            .Build();

        Host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
            .ConfigureHostConfiguration(builder =>
            {
                builder.AddConfiguration(configuration);
            })
            .ConfigureServices((context, services) =>
            {
                services.AddPubSub(configuration)
                    .AddConsumer<FrameworkEvent, FrameworkEventConsumer_SingleAccount, FrameworkEventDeadLetterConsumer_SingleAccount>("framework");

                services.AddSingleton<ConsumerCollector<FrameworkEvent, FrameworkEventConsumer_SingleAccount>>();
            }).Build();

        Host.Start();
    }

    public IMessageBus GetPublisher()
    {
        return Host.Services.GetRequiredService<IMessageBus>();
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

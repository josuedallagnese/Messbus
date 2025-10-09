using MessageBus.PubSub.Tests.Consumers;
using MessageBus.PubSub.Tests.Events;
using MessageBus.PubSub.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MessageBus.PubSub.Tests;

public class MultiAccountTests : IClassFixture<MultiAccountFixture>
{
    private readonly MultiAccountFixture _fixture;

    public MultiAccountTests(MultiAccountFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task MultiAccount_Publish_And_Consume()
    {
        await _fixture.Initialize();

        var environmentIdAccount1 = _fixture.EnvironmentIdAccount1;
        var environmentIdAccount2 = _fixture.EnvironmentIdAccount2;

        var provider = _fixture.Provider;

        var startTaks = provider.GetServices<IHostedService>()
            .Select(s => s.StartAsync(CancellationToken.None));

        var initializedCalls = 0;

        environmentIdAccount1.OnInitialized = async () =>
        {
            var message = FrameworkEvent.Create();

            var publisher = provider.GetRequiredKeyedService<IMessageBus>("Account1");

            await publisher.Publish("framework", message);

            initializedCalls++;
        };

        environmentIdAccount2.OnInitialized = async () =>
        {
            var message = FrameworkEvent.Create();

            var publisher = provider.GetRequiredKeyedService<IMessageBus>("Account2");

            await publisher.Publish("framework", message);
        };

        await Task.WhenAll(startTaks);

        var collectorAccount1 = provider.GetRequiredService<ConsumerCollector<FrameworkEvent, FrameworkEventConsumer_MultiAccount1>>();
        var collectorAccount2 = provider.GetRequiredService<ConsumerCollector<FrameworkEvent, FrameworkEventConsumer_MultiAccount2>>();

        await Task.WhenAll(
            collectorAccount1.WaitForItAsync(),
            collectorAccount2.WaitForItAsync());

        Assert.Equal(1, collectorAccount1.AttemptCount);
        Assert.Equal(1, collectorAccount2.AttemptCount);
        Assert.Equal(1, initializedCalls);
    }

    [Fact]
    public async Task MultiAccount_Publish_And_Consume_With_Retries()
    {
        await _fixture.Initialize();

        var environmentIdAccount1 = _fixture.EnvironmentIdAccount1;
        var environmentIdAccount2 = _fixture.EnvironmentIdAccount2;

        var provider = _fixture.Provider;

        var startTaks = provider.GetServices<IHostedService>()
            .Select(s => s.StartAsync(CancellationToken.None));

        var initializedCallsAccount1 = 0;

        environmentIdAccount1.OnInitialized = async () =>
        {
            var message = FrameworkEvent.Create();

            var publisher = provider.GetRequiredKeyedService<IMessageBus>("Account1");

            await publisher.Publish("framework", message);

            initializedCallsAccount1++;
        };

        var initializedCallsAccount2 = 0;

        environmentIdAccount2.OnInitialized = async () =>
        {
            var message = FrameworkEvent.Create();

            var publisher = provider.GetRequiredKeyedService<IMessageBus>("Account2");

            await publisher.Publish("framework", message);

            initializedCallsAccount2++;
        };

        await Task.WhenAll(startTaks);

        var collectorAccount1 = provider.GetRequiredService<ConsumerCollector<FrameworkEvent, FrameworkEventConsumer_MultiAccount1>>();
        var collectorAccount2 = provider.GetRequiredService<ConsumerCollector<FrameworkEvent, FrameworkEventConsumer_MultiAccount2>>();

        collectorAccount1.FailUntil = 2;
        collectorAccount2.FailUntil = 2;

        await Task.WhenAll(
            collectorAccount1.WaitForItAsync(),
            collectorAccount2.WaitForItAsync());

        Assert.Equal(3, collectorAccount1.AttemptCount);
        Assert.Equal(3, collectorAccount2.AttemptCount);
        Assert.Equal(1, initializedCallsAccount1);
        Assert.Equal(1, initializedCallsAccount2);
    }

    [Fact]
    public async Task MultiAccount_Publish_And_Consume_With_Retries_And_DeadLetter()
    {
        await _fixture.Initialize();

        var environmentIdAccount1 = _fixture.EnvironmentIdAccount1;
        var environmentIdAccount2 = _fixture.EnvironmentIdAccount2;

        var provider = _fixture.Provider;

        var startTaks = provider.GetServices<IHostedService>()
            .Select(s => s.StartAsync(CancellationToken.None));

        var initializedCallsAccount1 = 0;

        environmentIdAccount1.OnInitialized = async () =>
        {
            var message = FrameworkEvent.Create();

            var publisher = provider.GetRequiredKeyedService<IMessageBus>("Account1");

            await publisher.Publish("framework", message);

            initializedCallsAccount1++;
        };

        var initializedCallsAccount2 = 0;

        environmentIdAccount2.OnInitialized = async () =>
        {
            var message = FrameworkEvent.Create();

            var publisher = provider.GetRequiredKeyedService<IMessageBus>("Account2");

            await publisher.Publish("framework", message);

            initializedCallsAccount2++;
        };

        await Task.WhenAll(startTaks);

        var collectorAccount1 = provider.GetRequiredService<ConsumerCollector<FrameworkEvent, FrameworkEventConsumer_MultiAccount1>>();
        var collectorAccount2 = provider.GetRequiredService<ConsumerCollector<FrameworkEvent, FrameworkEventConsumer_MultiAccount2>>();

        collectorAccount1.FailUntil = 5;
        collectorAccount2.FailUntil = 5;

        await Task.WhenAll(
            collectorAccount1.WaitForItAsync(),
            collectorAccount2.WaitForItAsync());

        Assert.Equal(5, collectorAccount1.AttemptCount);
        Assert.False(collectorAccount1.Success);
        Assert.Equal(1, initializedCallsAccount1);

        Assert.Equal(5, collectorAccount2.AttemptCount);
        Assert.False(collectorAccount2.Success);
        Assert.Equal(1, initializedCallsAccount2);
    }
}

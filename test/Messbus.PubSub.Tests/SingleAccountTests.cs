using Messbus.PubSub.Tests.Consumers;
using Messbus.PubSub.Tests.Events;
using Messbus.PubSub.Tests.Fixtures;
using Messbus.PubSub.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;

namespace Messbus.PubSub.Tests;

public class SingleAccountTests : IClassFixture<SingleAccountFixture>
{
    private readonly SingleAccountFixture _fixture;

    public SingleAccountTests(SingleAccountFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task SingleAccount_Publish_And_Consume()
    {
        await _fixture.Initialize();

        var environmentId = _fixture.EnvironmentId;
        var provider = _fixture.Provider;

        var startTaks = provider.GetServices<IHostedService>()
            .Select(s => s.StartAsync(CancellationToken.None));

        var initializedCalls = 0;

        var message = FrameworkEvent.Create();

        environmentId.OnInitialized = async () =>
        {
            var publisher = provider.GetRequiredService<IMessageBus>();

            await publisher.Publish("framework", message);

            initializedCalls++;
        };

        await Task.WhenAll(startTaks);

        var collector = provider.GetRequiredService<ConsumerCollector<FrameworkEvent, FrameworkEventConsumer_SingleAccount>>();

        await collector.WaitForItAsync();

        Assert.Equal(1, collector.AttemptCount);
        Assert.Equal(1, initializedCalls);

        var expectedHash = HashHelper.ToMd5Hash(message);
        var hash = HashHelper.ToMd5Hash(collector.Event);
        Assert.Equal(expectedHash, hash);
    }

    [Fact]
    public async Task SingleAccount_Publish_And_Consume_With_Retries()
    {
        await _fixture.Initialize();

        var environmentId = _fixture.EnvironmentId;
        var provider = _fixture.Provider;

        var startTaks = provider.GetServices<IHostedService>()
            .Select(s => s.StartAsync(CancellationToken.None));

        var initializedCalls = 0;

        var message = FrameworkEvent.Create();

        environmentId.OnInitialized = async () =>
        {

            var publisher = provider.GetRequiredService<IMessageBus>();

            await publisher.Publish("framework", message);

            initializedCalls++;
        };

        await Task.WhenAll(startTaks);

        var collector = provider.GetRequiredService<ConsumerCollector<FrameworkEvent, FrameworkEventConsumer_SingleAccount>>();

        collector.FailUntil = 2;

        await collector.WaitForItAsync();

        Assert.Equal(3, collector.AttemptCount);
        Assert.Equal(1, initializedCalls);

        var expectedHash = HashHelper.ToMd5Hash(message);
        var hash = HashHelper.ToMd5Hash(collector.Event);
        Assert.Equal(expectedHash, hash);
    }

    [Fact]
    public async Task SingleAccount_Publish_And_Consume_With_Retries_And_DeadLetter()
    {
        await _fixture.Initialize();

        var environmentId = _fixture.EnvironmentId;
        var provider = _fixture.Provider;

        var startTaks = provider.GetServices<IHostedService>()
            .Select(s => s.StartAsync(CancellationToken.None));

        var initializedCalls = 0;

        environmentId.OnInitialized = async () =>
        {
            var message = FrameworkEvent.Create();

            var publisher = provider.GetRequiredService<IMessageBus>();

            await publisher.Publish("framework", message);

            initializedCalls++;
        };

        await Task.WhenAll(startTaks);

        var collector = provider.GetRequiredService<ConsumerCollector<FrameworkEvent, FrameworkEventConsumer_SingleAccount>>();

        collector.FailUntil = 5;

        await collector.WaitForItAsync();

        Assert.Equal(5, collector.AttemptCount);
        Assert.False(collector.Success);
        Assert.Equal(1, initializedCalls);
    }
}

using MessageBus.PubSub.Tests.Consumers;
using MessageBus.PubSub.Tests.Events;
using MessageBus.PubSub.Tests.Fixtures;
using MessageBus.PubSub.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;

namespace MessageBus.PubSub.Tests;

public class UnmanagedAccountTests : IClassFixture<UnmanagedAccountFixture>
{
    private readonly UnmanagedAccountFixture _fixture;

    public UnmanagedAccountTests(UnmanagedAccountFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task UnmanagedAccount_Publish_And_Consume()
    {
        await _fixture.Initialize();

        var environmentId = _fixture.EnvironmentId;
        var provider = _fixture.Provider;

        var startTaks = provider.GetServices<IHostedService>()
            .Select(s => s.StartAsync(CancellationToken.None));

        var initializedCalls = 0;

        var message = new UnmanagedEvent()
        {
            Id = Guid.NewGuid()
        };

        environmentId.OnInitialized = async () =>
        {
            var publisher = provider.GetRequiredService<IMessageBus>();

            await publisher.Publish("unmanaged", message);

            initializedCalls++;
        };

        await Task.WhenAll(startTaks);

        var collector = provider.GetRequiredService<ConsumerCollector<UnmanagedEvent, UnmanagedEventConsumer>>();

        await collector.WaitForItAsync();

        Assert.Equal(1, collector.AttemptCount);
        Assert.Equal(1, initializedCalls);

        var expectedHash = HashHelper.ToMd5Hash(message);
        var hash = HashHelper.ToMd5Hash(collector.Event);
        Assert.Equal(expectedHash, hash);
    }

    [Fact]
    public async Task UnmanagedAccount_Publish_And_Consume_With_Retries()
    {
        await _fixture.Initialize();

        var environmentId = _fixture.EnvironmentId;
        var provider = _fixture.Provider;

        var startTaks = provider.GetServices<IHostedService>()
            .Select(s => s.StartAsync(CancellationToken.None));

        var initializedCalls = 0;

        var message = new UnmanagedEvent()
        {
            Id = Guid.NewGuid()
        };

        environmentId.OnInitialized = async () =>
        {
            var publisher = provider.GetRequiredService<IMessageBus>();

            await publisher.Publish("unmanaged", message);

            initializedCalls++;
        };

        await Task.WhenAll(startTaks);

        var collector = provider.GetRequiredService<ConsumerCollector<UnmanagedEvent, UnmanagedEventConsumer>>();

        collector.FailUntil = 2;

        await collector.WaitForItAsync();

        Assert.Equal(3, collector.AttemptCount);
        Assert.Equal(1, initializedCalls);

        var expectedHash = HashHelper.ToMd5Hash(message);
        var hash = HashHelper.ToMd5Hash(collector.Event);
        Assert.Equal(expectedHash, hash);
    }
}

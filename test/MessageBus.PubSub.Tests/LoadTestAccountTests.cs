using MessageBus.PubSub.Tests.Consumers;
using MessageBus.PubSub.Tests.Events;
using MessageBus.PubSub.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MessageBus.PubSub.Tests;

public class LoadTestAccountTests : IClassFixture<LoadTestAccountFixture>
{
    private readonly LoadTestAccountFixture _fixture;

    public LoadTestAccountTests(LoadTestAccountFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task LoadTest_Send_Messages_Using_MaxOutstandingElementCount()
    {
        await _fixture.Initialize();

        var environmentId = _fixture.EnvironmentId;
        var provider = _fixture.Provider;

        var startTaks = provider.GetServices<IHostedService>()
            .Select(s => s.StartAsync(CancellationToken.None));

        var expectedMessageCount = 100;

        environmentId.OnInitialized = async () =>
        {
            var publisher = provider.GetRequiredService<IMessageBus>();

            var messages = Enumerable.Range(0, expectedMessageCount)
            .Select(i => new LoadEvent()
            {
                Id = i
            }).ToArray();

            await publisher.PublishBatch("load-test", messages);
        };

        await Task.WhenAll(startTaks);

        // Assert
        var collector = provider.GetRequiredService<ConsumerCollector<LoadEvent, LoadEventConsumer>>();

        collector.ExpectedMessageCount = expectedMessageCount;

        await collector.WaitForItAsync();

        Assert.Equal(expectedMessageCount, collector.MessageCount);
    }
}

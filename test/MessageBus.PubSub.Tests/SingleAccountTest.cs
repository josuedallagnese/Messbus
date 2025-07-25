using MessageBus.PubSub.Tests.Consumers;
using MessageBus.PubSub.Tests.Events;
using MessageBus.PubSub.Tests.Fixtures;

namespace MessageBus.PubSub.Tests;

public class SingleAccountTest : IClassFixture<SingleAccountFixture>
{
    private readonly SingleAccountFixture _fixture;

    public SingleAccountTest(SingleAccountFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task PublishAndConsume_WithoutFailures()
    {
        var publisher = _fixture.GetPublisher();
        var collector = _fixture.GetConsumerCollector<FrameworkEvent, FrameworkEventConsumer_SingleAccount>();

        var message = FrameworkEvent.Create();

        await publisher.Publish("framework", message);

        await collector.WaitForItAsync(TimeSpan.FromSeconds(60));

        collector = _fixture.GetConsumerCollector<FrameworkEvent, FrameworkEventConsumer_SingleAccount>();

        Assert.Equal(1, collector.AttemptCount);
    }

    [Fact]
    public async Task PublishAndConsume_WithRetries()
    {
        var publisher = _fixture.GetPublisher();
        var collector = _fixture.GetConsumerCollector<FrameworkEvent, FrameworkEventConsumer_SingleAccount>();

        collector.FailUntil = 2;

        var message = FrameworkEvent.Create();

        await publisher.Publish("framework", message);

        await collector.WaitForItAsync(TimeSpan.FromSeconds(60));

        collector = _fixture.GetConsumerCollector<FrameworkEvent, FrameworkEventConsumer_SingleAccount>();

        Assert.Equal(3, collector.AttemptCount);
    }

    [Fact]
    public async Task PublishAndConsume_WithRetriesAndDeadLettering()
    {
        var publisher = _fixture.GetPublisher();
        var collector = _fixture.GetConsumerCollector<FrameworkEvent, FrameworkEventConsumer_SingleAccount>();

        collector.FailUntil = 5;

        var message = FrameworkEvent.Create();

        await publisher.Publish("framework", message);

        await collector.WaitForItAsync(TimeSpan.FromSeconds(60));

        collector = _fixture.GetConsumerCollector<FrameworkEvent, FrameworkEventConsumer_SingleAccount>();

        Assert.Equal(5, collector.AttemptCount);
        Assert.False(collector.Success);
    }
}

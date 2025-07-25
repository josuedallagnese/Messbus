using MessageBus.PubSub.Tests.Consumers;
using MessageBus.PubSub.Tests.Events;
using MessageBus.PubSub.Tests.Fixtures;

namespace MessageBus.PubSub.Tests;

public class MultiAccountTest : IClassFixture<MultiAccountFixture>
{
    private readonly MultiAccountFixture _fixture;

    public MultiAccountTest(MultiAccountFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task PublishAndConsume_WithoutFailures()
    {
        var publisherAccount1 = _fixture.GetPublisher("Account1");
        var collectorAccount1 = _fixture.GetConsumerCollector<FrameworkEvent, FrameworkEventConsumer_MultiAccount1>();

        var publisherAccount2 = _fixture.GetPublisher("Account2");
        var collectorAccount2 = _fixture.GetConsumerCollector<FrameworkEvent, FrameworkEventConsumer_MultiAccount2>();

        var message = FrameworkEvent.Create();

        await publisherAccount1.Publish("framework", message);
        await publisherAccount2.Publish("framework", message);

        await Task.WhenAll(
            collectorAccount1.WaitForItAsync(TimeSpan.FromSeconds(60)),
            collectorAccount2.WaitForItAsync(TimeSpan.FromSeconds(60)));

        collectorAccount1 = _fixture.GetConsumerCollector<FrameworkEvent, FrameworkEventConsumer_MultiAccount1>();
        collectorAccount2 = _fixture.GetConsumerCollector<FrameworkEvent, FrameworkEventConsumer_MultiAccount2>();

        Assert.Equal(1, collectorAccount1.AttemptCount);
        Assert.Equal(1, collectorAccount2.AttemptCount);
    }

    [Fact]
    public async Task PublishAndConsume_WithRetries()
    {
        var publisherAccount1 = _fixture.GetPublisher("Account1");
        var collectorAccount1 = _fixture.GetConsumerCollector<FrameworkEvent, FrameworkEventConsumer_MultiAccount1>();

        collectorAccount1.FailUntil = 2;

        var publisherAccount2 = _fixture.GetPublisher("Account2");
        var collectorAccount2 = _fixture.GetConsumerCollector<FrameworkEvent, FrameworkEventConsumer_MultiAccount2>();

        collectorAccount2.FailUntil = 3;

        var message = FrameworkEvent.Create();

        await publisherAccount1.Publish("framework", message);
        await publisherAccount2.Publish("framework", message);

        await Task.WhenAll(
            collectorAccount1.WaitForItAsync(TimeSpan.FromSeconds(60)),
            collectorAccount2.WaitForItAsync(TimeSpan.FromSeconds(60)));

        collectorAccount1 = _fixture.GetConsumerCollector<FrameworkEvent, FrameworkEventConsumer_MultiAccount1>();
        collectorAccount2 = _fixture.GetConsumerCollector<FrameworkEvent, FrameworkEventConsumer_MultiAccount2>();

        Assert.Equal(3, collectorAccount1.AttemptCount);
        Assert.Equal(4, collectorAccount2.AttemptCount);
    }

    [Fact]
    public async Task PublishAndConsume_WithRetriesAndDeadLettering()
    {
        var publisherAccount1 = _fixture.GetPublisher("Account1");
        var collectorAccount1 = _fixture.GetConsumerCollector<FrameworkEvent, FrameworkEventConsumer_MultiAccount1>();

        collectorAccount1.FailUntil = 5;

        var publisherAccount2 = _fixture.GetPublisher("Account2");
        var collectorAccount2 = _fixture.GetConsumerCollector<FrameworkEvent, FrameworkEventConsumer_MultiAccount2>();

        collectorAccount2.FailUntil = 5;

        var message = FrameworkEvent.Create();

        await publisherAccount1.Publish("framework", message);
        await publisherAccount2.Publish("framework", message);

        await Task.WhenAll(
            collectorAccount1.WaitForItAsync(TimeSpan.FromSeconds(60)),
            collectorAccount2.WaitForItAsync(TimeSpan.FromSeconds(60)));

        collectorAccount1 = _fixture.GetConsumerCollector<FrameworkEvent, FrameworkEventConsumer_MultiAccount1>();
        collectorAccount2 = _fixture.GetConsumerCollector<FrameworkEvent, FrameworkEventConsumer_MultiAccount2>();

        Assert.False(collectorAccount1.Success);
        Assert.False(collectorAccount2.Success);
    }
}

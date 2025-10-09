using MessageBus.PubSub.Configuration;
using MessageBus.PubSub.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;

namespace MessageBus.PubSub.Tests;

public class ServiceCollectionExtensionTests
{
    [Fact]
    public void AddPubSub_WithValidConfiguration_RegistersServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.SingleAccount.json")
            .Build();

        // Act
        var builder = services.AddPubSub(configuration);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var messageBus = serviceProvider.GetService<IMessageBus>();
        var serializer = serviceProvider.GetService<IMessageSerializer>();

        Assert.NotNull(messageBus);
        Assert.IsType<PubSubMessageBus>(messageBus);
        Assert.NotNull(serializer);
        Assert.IsType<JsonMessageSerializer>(serializer);
    }

    [Fact]
    public void AddPubSub_WithAliasConfiguration_RegistersNamedServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.MultiAccount.json")
            .Build();

        // Act
        services.AddPubSub(configuration, "Account1");
        services.AddPubSub(configuration, "Account2");

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var messageBus1 = serviceProvider.GetKeyedService<IMessageBus>("Account1");
        var messageBus2 = serviceProvider.GetKeyedService<IMessageBus>("Account2");
        var serializer = serviceProvider.GetService<IMessageSerializer>();

        Assert.NotNull(messageBus1);
        Assert.NotNull(messageBus2);
        Assert.IsType<PubSubMessageBus>(messageBus1);
        Assert.IsType<PubSubMessageBus>(messageBus2);
        Assert.NotNull(serializer);
        Assert.IsType<JsonMessageSerializer>(serializer);
    }

    [Fact]
    public void AddPubSub_WithCustomConfiguration_RegistersServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddPubSub(config =>
        {
            config.ProjectId = "test-project";
            config.Publishing = new PublishingConfiguration
            {
                Prefix = "test",
                MessageRetentionDurationDays = 7,
                Topics = ["test-topic"]
            };
            config.Subscription = new SubscriptionConfiguration
            {
                Sufix = "test-subscription",
                MessageRetentionDurationDays = 7,
                AckDeadlineSeconds = 30,
                MaxDeliveryAttempts = 5,
                MinBackoffSeconds = 5,
                MaxBackoffSeconds = 600
            };
        });

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var messageBus = serviceProvider.GetService<IMessageBus>();
        var serializer = serviceProvider.GetService<IMessageSerializer>();

        Assert.NotNull(messageBus);
        Assert.IsType<PubSubMessageBus>(messageBus);
        Assert.NotNull(serializer);
        Assert.IsType<JsonMessageSerializer>(serializer);
    }

    [Fact]
    public void AddPubSub_WithInvalidConfiguration_ThrowsException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => services.AddPubSub(config =>
        {
            // Missing required configuration
            config.ProjectId = "";
            config.Publishing = new PublishingConfiguration
            {
                MessageRetentionDurationDays = 1 // Invalid retention days
            };
        }));
    }

    [Fact]
    public void AddPubSub_WithCustomSerializer_RegistersCustomSerializer()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.SingleAccount.json")
            .Build();

        // Act
        var builder = services.AddPubSub(configuration)
            .AddSerializer<CustomTestSerializer>();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var serializer = serviceProvider.GetService<IMessageSerializer>();

        Assert.NotNull(serializer);
        Assert.IsType<CustomTestSerializer>(serializer);
    }

    [Fact]
    public void AddPubSubConsumer_WithValidConfiguration_RegistersConsumer()
    {
        // Arrange
        var services = new ServiceCollection()
            .AddSingleton(new Mock<IHostApplicationLifetime>().Object);

        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.SingleAccount.json")
            .Build();

        // Act
        services.AddPubSub(configuration)
            .AddConsumer<TestEvent, TestEventConsumer>("framework");

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var consumer = serviceProvider.GetService<TestEventConsumer>();
        var hostedService = serviceProvider.GetServices<IHostedService>()
            .FirstOrDefault(x => x is MessageConsumer<TestEvent, TestEventConsumer>);

        Assert.NotNull(consumer);
        Assert.IsType<TestEventConsumer>(consumer);
        Assert.NotNull(hostedService);
    }

    [Fact]
    public void AddPubSubConsumer_WithDeadLetterQueue_RegistersBothConsumers()
    {
        // Arrange
        var services = new ServiceCollection()
            .AddSingleton(new Mock<IHostApplicationLifetime>().Object);

        // Act
        var builder = services.AddPubSub(config =>
        {
            config.ProjectId = "test-project";
            config.Publishing = new PublishingConfiguration
            {
                Prefix = "test",
                MessageRetentionDurationDays = 7,
                Topics = ["test-topic"]
            };
            config.Subscription = new SubscriptionConfiguration
            {
                Sufix = "test-subscription",
                MessageRetentionDurationDays = 7,
                AckDeadlineSeconds = 30,
                MaxDeliveryAttempts = 5,
                MinBackoffSeconds = 10,
                MaxBackoffSeconds = 600
            };
        });

        builder.AddConsumer<TestEvent, TestEventConsumer, TestEventDeadLetterConsumer>("test-topic");

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var consumer = serviceProvider.GetService<TestEventConsumer>();
        var consumerDeadLetter = serviceProvider.GetService<TestEventDeadLetterConsumer>();
        var hostedServices = serviceProvider.GetServices<IHostedService>()
            .Where(x => x is MessageConsumer<TestEvent, TestEventConsumer>
                || x is MessageConsumer<TestEvent, TestEventDeadLetterConsumer>);

        Assert.NotNull(consumer);
        Assert.NotNull(consumerDeadLetter);
        Assert.IsType<TestEventConsumer>(consumer);
        Assert.IsType<TestEventDeadLetterConsumer>(consumerDeadLetter);
        Assert.Equal(2, hostedServices.Count());
    }

    [Fact]
    public void AddPubSubConsumer_WithMultipleConsumers_RegistersAllConsumers()
    {
        // Arrange
        var services = new ServiceCollection()
            .AddSingleton(new Mock<IHostApplicationLifetime>().Object);

        // Act
        var builder = services.AddPubSub(config =>
        {
            config.ProjectId = "test-project";
            config.Publishing = new PublishingConfiguration
            {
                Prefix = "test",
                MessageRetentionDurationDays = 7,
                Topics = ["topic-1", "topic-2"]
            };
            config.Subscription = new SubscriptionConfiguration
            {
                Sufix = "test-subscription",
                MessageRetentionDurationDays = 7,
                AckDeadlineSeconds = 30,
                MaxDeliveryAttempts = 5,
                MinBackoffSeconds = 10,
                MaxBackoffSeconds = 600
            };
        });

        builder
            .AddConsumer<TestEvent1, TestEvent1Consumer>("topic-1")
            .AddConsumer<TestEvent2, TestEvent2Consumer>("topic-2");

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var consumer1 = serviceProvider.GetService<TestEvent1Consumer>();
        var consumer2 = serviceProvider.GetService<TestEvent2Consumer>();
        var hostedServices = serviceProvider.GetServices<IHostedService>();

        Assert.NotNull(consumer1);
        Assert.NotNull(consumer2);
        Assert.IsType<TestEvent1Consumer>(consumer1);
        Assert.IsType<TestEvent2Consumer>(consumer2);
        Assert.Equal(2, hostedServices.Count());
    }

    public record TestEvent(string Data);
    public record TestEvent1(string Data);
    public record TestEvent2(string Data);

    public class TestEventConsumer : IMessageConsumer<TestEvent>
    {
        public Task Handler(MessageContext<TestEvent> context, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    public class TestEventDeadLetterConsumer : IMessageConsumer<TestEvent>
    {
        public Task Handler(MessageContext<TestEvent> context, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    public class TestEvent1Consumer : IMessageConsumer<TestEvent1>
    {
        public Task Handler(MessageContext<TestEvent1> context, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    public class TestEvent2Consumer : IMessageConsumer<TestEvent2>
    {
        public Task Handler(MessageContext<TestEvent2> context, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}

public class CustomTestSerializer : IMessageSerializer
{
    public T Deserialize<T>(byte[] data) => default;

    public byte[] Serialize<T>(T message) => Array.Empty<byte>();
}

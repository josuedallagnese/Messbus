using MessageBus.PubSub.Configuration;
using MessageBus.PubSub.Internal;
using MessageBus.PubSub.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace MessageBus.PubSub;

public class PubSubMessageBusOptionsBuilder
{
    private readonly PubSubConfiguration _pubSubConfiguration;
    private readonly IServiceCollection _services;

    internal PubSubMessageBusOptionsBuilder(IServiceCollection services, PubSubConfiguration pubSubConfiguration)
    {
        _services = services;
        _pubSubConfiguration = pubSubConfiguration;

        _pubSubConfiguration.Validate();

        _services.AddLogging();

        if (string.IsNullOrWhiteSpace(_pubSubConfiguration.Alias))
            _services.TryAddSingleton<IMessageBus>((sp) => CreatePublisher(sp, _pubSubConfiguration));
        else
            _services.TryAddKeyedSingleton<IMessageBus>(_pubSubConfiguration.Alias, (sp, _) => CreatePublisher(sp, _pubSubConfiguration));

        _services.TryAddSingleton<IMessageSerializer, JsonMessageSerializer>();
    }

    internal PubSubMessageBusOptionsBuilder(IServiceCollection services, IConfiguration configuration, string alias) :
        this(services, new PubSubConfiguration(alias, configuration))
    {
    }

    public PubSubMessageBusOptionsBuilder AddSerializer<TSerializer>()
        where TSerializer : class, IMessageSerializer
    {
        _services.RemoveAll<IMessageSerializer>();
        _services.TryAddSingleton<IMessageSerializer, TSerializer>();

        return this;
    }

    public PubSubMessageBusOptionsBuilder AddConsumer<TEvent, TConsumer>(string topic)
        where TConsumer : class, IMessageConsumer<TEvent>
    {
        AddConsumer<TEvent, TConsumer>(topic, subscription: null, isDeadLetterConsumer: false);

        return this;
    }

    public PubSubMessageBusOptionsBuilder AddUnmanagedConsumer<TEvent, TConsumer>(string topic, string subscription)
        where TConsumer : class, IMessageConsumer<TEvent>
    {
        AddConsumer<TEvent, TConsumer>(topic, subscription, isDeadLetterConsumer: false);

        return this;
    }

    public PubSubMessageBusOptionsBuilder AddConsumer<TEvent, TConsumer, TDLConsumer>(string topic)
        where TConsumer : class, IMessageConsumer<TEvent>
        where TDLConsumer : class, IMessageConsumer<TEvent>
    {
        AddConsumer<TEvent, TConsumer>(topic, subscription: null, isDeadLetterConsumer: false);
        AddConsumer<TEvent, TDLConsumer>(topic, subscription: null, isDeadLetterConsumer: true);

        return this;
    }

    public PubSubMessageBusOptionsBuilder AddUnmanagedConsumer<TEvent, TConsumer, TDLConsumer>(string topic, string subscription)
        where TConsumer : class, IMessageConsumer<TEvent>
        where TDLConsumer : class, IMessageConsumer<TEvent>
    {
        AddConsumer<TEvent, TConsumer>(topic, subscription, isDeadLetterConsumer: false);
        AddConsumer<TEvent, TDLConsumer>(topic, subscription, isDeadLetterConsumer: true);

        return this;
    }

    private void AddConsumer<TEvent, TConsumer>(string topic, string subscription = null, bool isDeadLetterConsumer = false)
        where TConsumer : class, IMessageConsumer<TEvent>
    {
        _services.TryAddScoped<TConsumer>();

        var environmentId = _pubSubConfiguration.GetEnvironmentId(topic, subscription);
        var subscriptionId = _pubSubConfiguration.GetSubscriptionId(topic, subscription);

        if (isDeadLetterConsumer && subscriptionId.DeadLetterTopic is null)
            return;

        _services.AddHostedService((sp) => CreateConsumer<TEvent, TConsumer>(
            sp,
            subscriptionId,
            environmentId,
            isDeadLetterConsumer));
    }

    private static PubSubConsumer<TEvent, TConsumer> CreateConsumer<TEvent, TConsumer>(
        IServiceProvider sp,
        SubscriptionId subscriptionId,
        EnvironmentId environmentId,
        bool isDeadLetterConsumer)
        where TConsumer : class, IMessageConsumer<TEvent>
    {
        PubSubConsumer<TEvent, TConsumer> consumer = new(
            subscriptionId,
            environmentId,
            isDeadLetterConsumer,
            sp,
            sp.GetRequiredService<IMessageSerializer>(),
            sp.GetRequiredService<IHostApplicationLifetime>()
        );

        return consumer;
    }

    private static PubSubMessageBus CreatePublisher(
        IServiceProvider sp,
        PubSubConfiguration pubSubConfiguration)
    {
        return new PubSubMessageBus(
            pubSubConfiguration,
            sp,
            sp.GetRequiredService<IMessageSerializer>());
    }
}

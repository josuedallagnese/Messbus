using Grpc.Core;
using MessageBus.PubSub.Configuration;
using MessageBus.PubSub.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace MessageBus.PubSub;

public class PubSubMessageBusOptionsBuilder
{
    private readonly PubSubConfiguration _pubSubConfiguration;
    private readonly ChannelCredentials _channelCredentials;
    private readonly IServiceCollection _services;

    internal PubSubMessageBusOptionsBuilder(IServiceCollection services, PubSubConfiguration pubSubConfiguration)
    {
        _services = services;
        _pubSubConfiguration = pubSubConfiguration;

        _pubSubConfiguration.Validate();

        _channelCredentials = _pubSubConfiguration.GetChannelCredentials();

        _services.AddLogging();

        if (string.IsNullOrWhiteSpace(_pubSubConfiguration.Alias))
            _services.TryAddSingleton<IMessageBus>((sp) => CreatePublisher(sp, _pubSubConfiguration));
        else
            _services.TryAddKeyedSingleton<IMessageBus>(_pubSubConfiguration.Alias, (sp, _) => CreatePublisher(sp, _pubSubConfiguration));

        _services.TryAddSingleton<IPubSubSerializer, PubSubSerializer>();;
    }

    internal PubSubMessageBusOptionsBuilder(IServiceCollection services, IConfiguration configuration, string alias) :
        this(services, new PubSubConfiguration(alias, configuration))
    {
    }

    public PubSubMessageBusOptionsBuilder AddSerializer<TSerializer>()
        where TSerializer : class, IPubSubSerializer
    {
        _services.RemoveAll<IPubSubSerializer>();
        _services.TryAddSingleton<IPubSubSerializer, TSerializer>();

        return this;
    }

    public PubSubMessageBusOptionsBuilder AddConsumer<TEvent, TConsumer>(string topic)
        where TConsumer : class, IMessageConsumer<TEvent>
    {
        AddConsumer<TEvent, TConsumer>(topic, isDeadLetter: false);

        return this;
    }

    public PubSubMessageBusOptionsBuilder AddConsumer<TEvent, TConsumer, TDLConsumer>(string topic)
        where TConsumer : class, IMessageConsumer<TEvent>
        where TDLConsumer : class, IMessageConsumer<TEvent>
    {
        AddConsumer<TEvent, TConsumer>(topic, isDeadLetter: false);
        AddConsumer<TEvent, TDLConsumer>(topic, isDeadLetter: true);

        return this;
    }

    private void AddConsumer<TEvent, TConsumer>(string topic, bool isDeadLetter)
        where TConsumer : class, IMessageConsumer<TEvent>
    {
        _services.TryAddScoped<TConsumer>();

        var subcriptionId = _pubSubConfiguration.GetSubscriptionId(topic);

        _services.AddHostedService((sp) => CreateConsumer<TEvent, TConsumer>(sp, _channelCredentials, subcriptionId, isDeadLetter));
    }

    private static PubSubConsumer<TEvent, TConsumer> CreateConsumer<TEvent, TConsumer>(
        IServiceProvider sp,
        ChannelCredentials credential,
        SubscriptionId subcriptionId,
        bool isDeadLetter)
        where TConsumer : class, IMessageConsumer<TEvent>
    {
        return new PubSubConsumer<TEvent, TConsumer>(
            credential,
            subcriptionId,
            isDeadLetter,
            sp.GetRequiredService<IServiceScopeFactory>(),
            sp.GetRequiredService<IPubSubSerializer>(),
            sp.GetRequiredService<ILogger<PubSubConsumer<TEvent, TConsumer>>>());
    }

    private static PubSubMessageBus CreatePublisher(
        IServiceProvider sp,
        PubSubConfiguration pubSubConfiguration)
    {
        return new PubSubMessageBus(
            pubSubConfiguration,
            sp.GetRequiredService<IServiceScopeFactory>(),
            sp.GetRequiredService<IPubSubSerializer>(),
            sp.GetRequiredService<ILogger<PubSubMessageBus>>());
    }
}

using Messbus.PubSub.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Messbus.PubSub;

public static class ServiceCollectionExtension
{
    public static PubSubMessageBusOptionsBuilder AddPubSub(
        this IServiceCollection services,
        IConfiguration configuration,
        string alias = null)
    {
        return new PubSubMessageBusOptionsBuilder(services, configuration, alias);
    }

    public static PubSubMessageBusOptionsBuilder AddPubSub(
        this IServiceCollection services,
        Action<PubSubConfiguration> configuration)
    {
        var pubSubConfiguration = new PubSubConfiguration();

        configuration?.Invoke(pubSubConfiguration);

        return AddPubSub(services, pubSubConfiguration);
    }

    public static PubSubMessageBusOptionsBuilder AddPubSub(
        this IServiceCollection services,
        PubSubConfiguration pubSubConfiguration)
    {
        return new PubSubMessageBusOptionsBuilder(services, pubSubConfiguration);
    }
}

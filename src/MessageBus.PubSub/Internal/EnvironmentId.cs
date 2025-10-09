using Google.Api.Gax;
using Google.Cloud.Iam.V1;
using Google.Cloud.PubSub.V1;
using Google.Protobuf.Collections;
using Grpc.Core;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MessageBus.PubSub.Internal;

public class EnvironmentId
{
    private readonly PublisherServiceApiClient _publisherService;
    private readonly SubscriberServiceApiClient _subscriberService;
    private readonly ChannelCredentials _channelCredentials;
    private readonly FlowControlSettings _flowControlSettings;

    public bool UseEmulator { get; }
    public bool Unmanaged { get; }
    public bool VerbosityMode { get; }

    public Func<Task> OnInitialized { get; set; }

    public EnvironmentId(
        bool useEmulator,
        bool unmanaged,
        bool verbosityMode,
        ChannelCredentials channelCredentials,
        FlowControlSettings flowControlSettings)
    {
        UseEmulator = useEmulator;
        Unmanaged = unmanaged;
        VerbosityMode = verbosityMode;

        _channelCredentials = channelCredentials;
        _flowControlSettings = flowControlSettings;
        _publisherService = GetPublisher();
        _subscriberService = GetSubscriber();
    }

    internal void Initialized()
    {
        if (OnInitialized is not null)
            _ = Task.Run(OnInitialized);
    }

    private PublisherServiceApiClient GetPublisher()
    {
        var builder = new PublisherServiceApiClientBuilder();

        if (UseEmulator)
            builder.EmulatorDetection = EmulatorDetection.EmulatorOnly;
        else
            builder.ChannelCredentials = _channelCredentials;

        return builder.Build();
    }

    private SubscriberServiceApiClient GetSubscriber()
    {
        var builder = new SubscriberServiceApiClientBuilder();

        if (UseEmulator)
            builder.EmulatorDetection = EmulatorDetection.EmulatorOnly;
        else
            builder.ChannelCredentials = _channelCredentials;

        return builder.Build();
    }

    internal SubscriberClient GetSubscriberClient(SubscriptionName subscriptionName)
    {
        var builder = new SubscriberClientBuilder()
        {
            SubscriptionName = subscriptionName
        };

        if (UseEmulator)
            builder.EmulatorDetection = EmulatorDetection.EmulatorOnly;
        else
            builder.ChannelCredentials = _channelCredentials;

        if (_flowControlSettings is not null)
        {
            builder.Settings = new SubscriberClient.Settings()
            {
                FlowControlSettings = _flowControlSettings,
            };
        }

        return builder.Build();
    }

    internal async Task<RepeatedField<string>> Publish(TopicName topicName, IEnumerable<PubsubMessage> messages, ILogger logger, CancellationToken stoppingToken = default)
    {
        try
        {
            var response = await _publisherService.PublishAsync(topicName, messages, stoppingToken);

            var dataLength = response.CalculateSize();
            var messageIds = response.MessageIds;

            logger.LogInformation("Message=Messages {Count} sent successfully to the topic {TopicName}; ProjectId={ProjectId}, DataLength={DataLength}",
                messageIds.Count,
                topicName,
                topicName.ProjectId,
                dataLength);

            return messageIds;
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            logger.LogError("Message=Topic {TopicName} not found; ProjectId={ProjectId}",
                topicName,
                topicName.ProjectId);

            throw;
        }
    }

    internal async Task CreateTopic(Topic topic, ILogger logger, CancellationToken stoppingToken = default)
    {
        try
        {
            await _publisherService.CreateTopicAsync(
                topic,
                stoppingToken);

            logger.LogInformation("Message=Topic {TopicName} was created; ProjectId={ProjectId};",
                topic.Name,
                topic.TopicName.ProjectId);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.AlreadyExists)
        {
            logger.LogInformation("Message=Topic {TopicName} already exists; ProjectId={ProjectId};",
                topic.Name,
                topic.TopicName.ProjectId);
        }
    }

    internal async Task CreateSubscription(Subscription subscription, ILogger logger, CancellationToken stoppingToken = default)
    {
        try
        {
            await _subscriberService.CreateSubscriptionAsync(subscription, stoppingToken);

            logger.LogInformation("Message=Subscription {Name} was created; ProjectId={ProjectId};",
                subscription.Name,
                subscription.SubscriptionName.ProjectId);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.AlreadyExists)
        {
            logger.LogInformation("Message=Subscription {Name} already exists; ProjectId={ProjectId};",
                subscription.Name,
                subscription.SubscriptionName.ProjectId);
        }
    }

    internal async Task AddRoleBindings(SubscriptionId subscriptionId, ILogger logger, CancellationToken stoppingToken = default)
    {
        if (UseEmulator)
        {
            logger.LogInformation("Role bindings not working when emulator is enabled");
            return;
        }

        if (Unmanaged)
        {
            logger.LogInformation("Role bindings not working when topics and subscriptions is Unmanaged resources");
            return;
        }

        var serviceAccount = await subscriptionId.GetServiceAccount();
        var resource = subscriptionId.DeadLetterTopic.Name.ToString();

        try
        {


            var policy = await _publisherService.IAMPolicyClient.GetIamPolicyAsync(new GetIamPolicyRequest { Resource = resource }, stoppingToken);

            if (AppendRole(policy, "roles/pubsub.publisher", serviceAccount))
            {
                await _publisherService.IAMPolicyClient.SetIamPolicyAsync(new SetIamPolicyRequest { Resource = resource, Policy = policy }, stoppingToken);

                logger.LogInformation("Message=Granted roles/pubsub.publisher on DLQ topic; Resource={Resource}; ServiceAccount={serviceAccount}", resource, serviceAccount);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Message=Failed to grant roles/pubsub.publisher on DLQ topic; Resource={Resource};", resource);
            throw;
        }

        resource = subscriptionId.Subscription.SubscriptionName.ToString();

        try
        {
            var policy = await _subscriberService.IAMPolicyClient.GetIamPolicyAsync(new GetIamPolicyRequest { Resource = resource }, stoppingToken);

            if (AppendRole(policy, "roles/pubsub.subscriber", serviceAccount))
            {
                await _subscriberService.IAMPolicyClient.SetIamPolicyAsync(new SetIamPolicyRequest { Resource = resource, Policy = policy }, stoppingToken);

                logger.LogInformation("Message=Granted roles/pubsub.subscriber on subscription; Resource={Resource}; ServiceAccount={serviceAccount}", resource, serviceAccount);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Message=Failed to granted roles/pubsub.subscriber on subscription; Resource={Resource};", resource);
            throw;
        }
    }

    internal async Task EnsureSubscriptionExists(SubscriptionName subscriptionName, ILogger logger, int maxAttempts, int delayInSeconds, IHostApplicationLifetime lifetime, CancellationToken stoppingToken = default)
    {
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                var subscription = await _subscriberService.GetSubscriptionAsync(subscriptionName, cancellationToken: stoppingToken);
                if (subscription is not null)
                {
                    logger.LogInformation(
                        "Message=Subscription {SubscriptionName} found on attempt {Attempt}. Proceeding with consumer initialization.",
                        subscriptionName,
                        attempt);

                    return;
                }
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
            {
                logger.LogWarning(
                    "Message=Subscription {SubscriptionName} not found. Attempt {Attempt}/{MaxAttempts}. Retrying in {Delay}s...",
                    subscriptionName,
                    attempt,
                    maxAttempts,
                    delayInSeconds);
            }

            if (attempt < maxAttempts)
                await Task.Delay(TimeSpan.FromSeconds(delayInSeconds), stoppingToken);
        }

        logger.LogCritical(
            "Message=Subscription {SubscriptionName} not found after {MaxAttempts} attempts. Shutting down process.",
            subscriptionName,
            maxAttempts);

        lifetime.StopApplication();
    }

    internal async Task DeleteSubscriptions(SubscriptionId subscriptionId, ILogger logger, CancellationToken stoppingToken = default)
    {
        var deadLetter = subscriptionId.DeadLetterSubscription;
        var subscription = subscriptionId.Subscription;

        if (deadLetter is not null)
        {
            try
            {
                logger.LogInformation("Deleting subscription {SubscriptionName}", deadLetter.SubscriptionName);

                await _subscriberService.DeleteSubscriptionAsync(new DeleteSubscriptionRequest
                {
                    SubscriptionAsSubscriptionName = deadLetter.SubscriptionName
                }, stoppingToken);
            }
            catch (Exception) { }
        }

        if (subscription is not null)
        {
            try
            {
                logger.LogInformation("Deleting subscription {SubscriptionName}", subscription.SubscriptionName);

                await _subscriberService.DeleteSubscriptionAsync(new DeleteSubscriptionRequest
                {
                    SubscriptionAsSubscriptionName = subscription.SubscriptionName
                }, stoppingToken);
            }
            catch (Exception) { }
        }
    }

    internal async Task DeleteTopics(SubscriptionId subscriptionId, ILogger logger, CancellationToken stoppingToken = default)
    {
        var deadLetterTopicName = subscriptionId.DeadLetterTopic?.TopicName;
        var topicName = subscriptionId.TopicId.TopicName;

        if (deadLetterTopicName is not null)
        {
            try
            {
                logger.LogInformation("Deleting topic {TopicName}", deadLetterTopicName);

                await _publisherService.DeleteTopicAsync(new DeleteTopicRequest
                {
                    TopicAsTopicName = deadLetterTopicName
                }, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "The topic {TopicName} cannot be deleted", deadLetterTopicName);
            }
        }

        if (topicName is not null)
        {
            try
            {
                logger.LogInformation("Deleting topic {TopicName}", topicName);

                await _publisherService.DeleteTopicAsync(new DeleteTopicRequest
                {
                    TopicAsTopicName = topicName
                }, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "The topic {TopicName} cannot be deleted", topicName);
            }
        }
    }

    private static bool AppendRole(Policy policy, string role, string member)
    {
        var binding = policy.Bindings.FirstOrDefault(b => b.Role == role);
        if (binding is null)
        {
            binding = new Binding
            {
                Role = role
            };

            policy.Bindings.Add(binding);
        }

        if (!binding.Members.Contains(member))
        {
            binding.Members.Add(member);
            return true;
        }

        return false;
    }
}

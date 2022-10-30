using Amazon.SimpleNotificationService;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PubSub.Common;

namespace PubSub.Subscribe;

public interface ISubscriberConfiguration
{
    Task<string> EnsureQueuesExist(CancellationToken cancellationToken);
    Task EnsureSubscriptionExists<T>(CancellationToken cancellationToken);
}
public class SubscriberConfiguration : ISubscriberConfiguration
{
    private readonly IAmazonSQS _sqs;
    private readonly IAmazonSimpleNotificationService _sns;
    private readonly ILogger<SubscriberConfiguration> _log;
    private readonly IConfiguration _configuration;

    private const string MaxReceiveCount = "5";// This many attempts
    private const string MessageRetentionPeriod = "1209600";// 14 days

    public SubscriberConfiguration(IAmazonSQS sqs, IAmazonSimpleNotificationService sns, ILogger<SubscriberConfiguration> log, IConfiguration configuration)
    {
        _sqs = sqs;
        _sns = sns;
        _log = log;
        _configuration = configuration;
    }

    public async Task<string> EnsureQueuesExist(CancellationToken cancellationToken)
    {
        // Setup source queue
        var sourceName = _configuration.GetQueueName();
        _log.LogInformation("Ensuring sqs queue exists: {QueueName}", sourceName);
        var (sourceExists, sourceUrl) = await QueueExists(sourceName, cancellationToken);
        if (!sourceExists)
        {
            sourceUrl = await CreateQueue(sourceName, cancellationToken);
        }
        await SetRetentionPeriod(sourceUrl!);

        // Setup the dead letter queue
        var deadName = _configuration.GetDeadLetterQueueName();
        _log.LogInformation("Ensuring sqs dead letter queue exists {QueueName}", deadName);
        var (deadExists, deadUrl) = await QueueExists(deadName, cancellationToken);
        if (!deadExists)
        {
            deadUrl = await CreateQueue(deadName, cancellationToken);
        }
        await SetRetentionPeriod(deadUrl!);
        
        // Setup the redrive policy between from the source queue to the dead letter queue
        _log.LogInformation("Ensuring sqs redrive policy exists");
        await SetRedrivePolicy(sourceUrl!, deadUrl!, cancellationToken);

        return sourceUrl!;
    }

    public async Task EnsureSubscriptionExists<T>(CancellationToken cancellationToken)
    {
        var topicName = _configuration.GetTopicName<T>();
        var queueName = _configuration.GetQueueName();

        _log.LogInformation("Ensuring sns subscription to topic {TopicArn} exists", topicName);
        var (topicExists, topicArn) = await TopicExists(topicName, cancellationToken);
        if (!topicExists)
        {
            throw new Exception($"Topic does not exist: {topicName}");
        }

        var (queueExists, queueUrl) = await QueueExists(queueName, cancellationToken);
        if (!queueExists)
        {
            throw new Exception($"Queue does not exist: {queueName}");
        }

        var (subscriptionExists, subscriptionArn) = await SubscriptionExists(topicArn!, queueUrl!, cancellationToken);
        if (!subscriptionExists)
        {
            _log.LogInformation("Subscribing sqs queue {QueueUrl} to sns topic {TopicArn}", queueUrl!, topicArn);
            var response = await _sns.SubscribeQueueToTopicsAsync(new List<string> {topicArn!}, _sqs, queueUrl!);
            subscriptionArn = response.Values.Single();
        }

        _log.LogInformation("Setting attributes on subscription {SubscriptionArn}", subscriptionArn);
        await _sns.SetSubscriptionAttributesAsync(subscriptionArn, "RawMessageDelivery", "true", cancellationToken);
    }

    private async Task<string> CreateQueue(string name, CancellationToken cancellationToken)
    {
        _log.LogInformation("Creating sqs queue: {QueueName}", name);
        var response = await _sqs.CreateQueueAsync(name, cancellationToken);
        return response.QueueUrl;
    }

    private async Task<(bool exists, string? arn)> TopicExists(string name, CancellationToken cancellationToken)
    {
        var response = await _sns.FindTopicAsync(name);
        if (response == null || string.IsNullOrEmpty(response.TopicArn))
        {
            _log.LogInformation("No sns topic exists for {TopicName}", name);
            return (false, null);
        }
        _log.LogInformation("A sns topic exists for {TopicName}: {TopicArn}", name, response.TopicArn);
        return (true, arn: response.TopicArn);
    }

    private async Task<(bool exists, string? url)> QueueExists(string name, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _sqs.GetQueueUrlAsync(name, cancellationToken);
            if (response == null || string.IsNullOrEmpty(response.QueueUrl))
            {
                _log.LogInformation("No sqs queue exists for {TopicName}", name);
                return (false, null);
            }
            _log.LogInformation("A sqs queue exists for {TopicName}: {TopicArn}", name, response.QueueUrl);
            return (true, response.QueueUrl);
        }
        catch (QueueDoesNotExistException)
        {
            _log.LogInformation("No sqs queue exists for {QueueName}", name);
            return (false, null);
        }
    }

    private async Task<(bool exists, string? arn)> SubscriptionExists(string topicArn, string queueUrl, CancellationToken cancellationToken)
    {
        // Get the queue arn because that is what is registered on the subscription
        var queueResponse = await _sqs.GetQueueAttributesAsync(queueUrl, new List<string> {QueueAttributeName.QueueArn}, cancellationToken);
        var queueArn = queueResponse.QueueARN;

        // limited to 100
        var response = await _sns.ListSubscriptionsByTopicAsync(topicArn, cancellationToken);
        var subscription = response.Subscriptions.SingleOrDefault(x => x.Endpoint == queueArn && x.Protocol == "sqs");
        if (subscription == null)
        {
            _log.LogInformation("No sns subscription exists for sns topic {TopicArn} to sqs queue {QueueUrl}", topicArn, queueUrl);
            return (false, null);
        }
        _log.LogInformation("A sns subscription exists for sns topic {TopicArn} to sqs queue {QueueUrl}: {SubscriptionArn}", topicArn, queueUrl, subscription.SubscriptionArn);
        return (true, subscription.SubscriptionArn);
    }

    private async Task SetRedrivePolicy(string queueUrl, string deadUrl, CancellationToken cancellationToken)
    {
        var deadArn = await GetQueueArn(deadUrl, cancellationToken);
        _log.LogInformation("Settings redrive policy from {QueueUrl} to {QueueName}", queueUrl, deadArn);
        await SetUpdateAttribute(queueUrl, QueueAttributeName.RedrivePolicy, $"{{\"deadLetterTargetArn\":\"{deadArn}\",\"maxReceiveCount\":\"{MaxReceiveCount}\"}}");
    }

    private async Task SetRetentionPeriod(string queueUrl)
    {
        _log.LogInformation("Setting retention period for queue {QueueUrl}", queueUrl);
        await SetUpdateAttribute(queueUrl, QueueAttributeName.MessageRetentionPeriod, MessageRetentionPeriod);
    }

    private async Task<string> GetQueueArn(string? deadUrl, CancellationToken cancellationToken)
    {
        var response = await _sqs.GetQueueAttributesAsync(deadUrl, new List<string> {QueueAttributeName.QueueArn}, cancellationToken);
        var deadArn = response.QueueARN;
        return deadArn;
    }

    private async Task SetUpdateAttribute(string url, QueueAttributeName attribute, string value) =>
        await _sqs.SetQueueAttributesAsync(url, new Dictionary<string, string> {{attribute.Value, value}});
}
using Amazon.SimpleNotificationService;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PubSub.Common;

namespace PubSub.Publish;

public interface IPublisher
{
    Task PublishToTopic<T>(string message, CancellationToken cancellationToken);
}
public class Publisher : IPublisher
{
    private readonly IAmazonSimpleNotificationService _sns;
    private readonly ILogger<Publisher> _log;
    private readonly IMemoryCache _cache;
    private readonly IConfiguration _configuration;

    public Publisher(IAmazonSimpleNotificationService sns, ILogger<Publisher> log, IMemoryCache cache, IConfiguration configuration)
    {
        _sns = sns;
        _log = log;
        _cache = cache;
        _configuration = configuration;
    }
    public async Task PublishToTopic<T>(string message, CancellationToken cancellationToken)
    {
        var messageType = typeof(T).Name;
        var topicName = _configuration.GetTopicName<T>();
        var topicArn = await GetTopicArnCached(topicName);
        _log.LogInformation("Publishing a message of type {MessageType} to topic {TopicName}", messageType, topicName);
        await _sns.PublishAsync(topicArn, message, cancellationToken);
    }

    private async Task<string> GetTopicArnCached(string topicName)
    {
        _log.LogDebug("Looking up sns topic arn for {TopicName}", topicName);
        var arn = await _cache.GetOrCreateAsync<string>(
            topicName, async _ => {
                var response = await _sns.FindTopicAsync(topicName);
                if (response == null || string.IsNullOrEmpty(response.TopicArn))
                {
                    throw new Exception($"No sns topics exists for {topicName}");
                }
                return response.TopicArn;
            });
        _log.LogDebug("Found sns topic arn for {TopicName}: {TopicArn}", topicName, arn);
        return arn;
    }
}
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PubSub.Extensions;

namespace PubSub.Subscribing;

internal class Sub : ISub
{
    private readonly IAmazonSQS _sqs;
    private readonly ILogger<Sub> _log;
    private readonly IMemoryCache _cache;
    private readonly IConfiguration _configuration;

    public Sub(IAmazonSQS sqs, ILogger<Sub> log, IMemoryCache cache, IConfiguration configuration)
    {
        _sqs = sqs;
        _log = log;
        _cache = cache;
        _configuration = configuration;
    }

    public async Task<IReadOnlyCollection<Message>> GetMessagesFromQueue(CancellationToken cancellationToken)
    {
        var queueName = _configuration.GetQueueName();
        var queueUrl = await GetQueueUrlCached(queueName, cancellationToken);
        var response = await _sqs.ReceiveMessageAsync(queueUrl, cancellationToken);
        _log.LogDebug("Received {MessagesCount} messages from sqs queue {QueueUrl}", response.Messages.Count, queueUrl);
        return response.Messages;
    }

    public async Task DeleteMessageFromQueue(string receiptHandle, CancellationToken cancellationToken)
    {
        var queueName = _configuration.GetQueueName();
        var queueUrl = await GetQueueUrlCached(queueName, cancellationToken);
        _log.LogInformation("Deleting message {ReceiptHandle} from sqs queue {QueueUrl}", receiptHandle, queueUrl);
        await _sqs.DeleteMessageAsync(queueUrl, receiptHandle, cancellationToken);
    }

    private async Task<string> GetQueueUrlCached(string queueName, CancellationToken cancellationToken)
    {
        _log.LogDebug("Looking up sqs queue url for {QueueName}", queueName);
        var queueUrl = await _cache.GetOrCreateAsync<string>(
            queueName, async _ => {
                var response = await _sqs.GetQueueUrlAsync(queueName, cancellationToken);
                if (response == null || string.IsNullOrEmpty(response.QueueUrl))
                {
                    throw new Exception($"No sqs queue exists for {queueName}");
                }
                return response.QueueUrl;
            });
        _log.LogDebug("Found sqs queue url for {QueueName}: {QueueUrl}", queueName, queueUrl);
        return queueUrl;
    }
}
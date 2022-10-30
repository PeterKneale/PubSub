using Amazon.SimpleNotificationService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PubSub.Extensions;

namespace PubSub.Publishing;

internal class ConfigurePub : IConfigurePub
{
    private readonly IAmazonSimpleNotificationService _sns;
    private readonly ILogger<ConfigurePub> _log;
    private readonly IConfiguration _configuration;

    public ConfigurePub(IAmazonSimpleNotificationService sns, ILogger<ConfigurePub> log, IConfiguration configuration)
    {
        _sns = sns;
        _log = log;
        _configuration = configuration;
    }
   
    public async Task<string> EnsureTopicExists<T>(CancellationToken cancellationToken)
    {
        var name = _configuration.GetTopicName<T>();
        _log.LogInformation("Ensuring sns topic exists: {TopicName}", name);
        var (exists, arn) = await TopicExists(name, cancellationToken);
        if (exists) return arn!;
        return await CreateTopic(name, cancellationToken);
    }

    private async Task<string> CreateTopic(string name, CancellationToken cancellationToken)
    {
        _log.LogInformation("Creating sns topic: {TopicName}", name);
        var response = await _sns.CreateTopicAsync(name, cancellationToken);
        return response.TopicArn;
    }
    
    private async Task<(bool exists, string? arn)> TopicExists(string name, CancellationToken cancellationToken)
    {
        _log.LogInformation("Checking sns topic exists {TopicName}", name);
        var response = await _sns.FindTopicAsync(name);
        if (response == null || string.IsNullOrEmpty(response.TopicArn))
        {
            _log.LogInformation("No sns topic exists for {TopicName}", name);
            return (false, null);
        }
        _log.LogInformation("A sns topic exists for {TopicName}: {TopicArn}", name, response.TopicArn);
        return (true, arn: response.TopicArn);
    }
}
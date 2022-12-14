using Microsoft.Extensions.Configuration;

namespace PubSub.Common;

internal static class ConfigurationExtensions
{
    const string PubSubPrefix = "pubsub:prefix";
    const string PubSubService = "pubsub:service";

    private const int MaxTopicNameLength = 256;
    private const int MaxQueueNameLength = 80;

    // {prefix}-{topic}
    public static string GetTopicName<T>(this IConfiguration configuration) =>
        $"{configuration.Prefix()}-{typeof(T).Name}"
            .ToLowerInvariant()
            .TrimTo(MaxTopicNameLength);

    // {prefix}-{service}
    public static string GetQueueName(this IConfiguration configuration) =>
        $"{configuration.Prefix()}-{configuration.Service()}"
            .ToLowerInvariant()
            .TrimTo(MaxQueueNameLength);

    // {prefix}-{service}-dlq
    public static string GetDeadLetterQueueName(this IConfiguration configuration) =>
        $"{configuration.Prefix()}-{configuration.Service()}-dlq"
            .ToLowerInvariant()
            .TrimTo(MaxQueueNameLength);

    private static string Prefix(this IConfiguration configuration) =>
        configuration[PubSubPrefix] ?? throw new Exception($"Configuration setting '{PubSubPrefix}' is missing");

    private static string Service(this IConfiguration configuration) =>
        configuration[PubSubService] ?? throw new Exception($"Configuration setting '{PubSubService}' is missing");

    private static string TrimTo(this string s, int maximumLength) =>
        s.Length <= maximumLength
            ? s
            : s[..maximumLength];
}
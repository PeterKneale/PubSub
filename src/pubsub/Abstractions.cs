using Amazon.SQS.Model;

namespace PubSub;

public interface IPub
{
    Task PublishToTopic<T>(string message, CancellationToken cancellationToken);
}

public interface IConfigurePub
{
    Task<string> EnsureTopicExists<T>(CancellationToken cancellationToken);
}

public interface ISub
{
    Task<IReadOnlyCollection<Message>> GetMessagesFromQueue(CancellationToken cancellationToken);
    Task DeleteMessageFromQueue(string receiptHandle, CancellationToken cancellationToken);
}
public interface ISubMessageHandler
{
    Task Handle(Message message, CancellationToken cancellationToken);
}
public interface IConfigureSub
{
    Task<string> EnsureQueueExists(CancellationToken cancellationToken);
    Task<string> EnsureDeadLetterQueueExists(CancellationToken cancellationToken);
    Task EnsureSubscriptionExists<T>(CancellationToken cancellationToken);
}
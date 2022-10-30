using Amazon.SQS.Model;

namespace PubSub.Subscribe;

public interface ISubscriberMessageHandler
{
    Task Handle(Message submittedMessage, CancellationToken cancellationToken);
}
using Amazon.SQS.Model;

namespace PubSub.Subscribe;

public interface ISubscriberMessageHandler
{
    Task Handle(Message message, CancellationToken cancellationToken);
}
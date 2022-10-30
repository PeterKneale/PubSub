using Amazon.SQS.Model;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PubSub.Publish;
using PubSub.Subscribe;
using Samples.Messages;

namespace Samples.PublisherAndSubscriber;

public class Handler : ISubscriberMessageHandler
{
    private readonly IPublisher _publisher;
    private readonly ILogger<Handler> _log;

    public Handler(IPublisher publisher, ILogger<Handler> log)
    {
        _publisher = publisher;
        _log = log;
    }

    public Task Handle(Message submittedMessage, CancellationToken cancellationToken)
    {
        var orderSubmittedEvent = JsonConvert.DeserializeObject<OrderSubmittedEvent>(submittedMessage.Body);
        _log.LogInformation("*** Processing order {OrderId}", orderSubmittedEvent.OrderId);
        
        // process the order 
        // .... 
        // .... 
        // .... 
        // .... 
        _log.LogInformation("*** Processed order {OrderId}", orderSubmittedEvent.OrderId);

        var orderCompletedEvent = JsonConvert.SerializeObject(new OrderCompletedEvent {OrderId = Guid.NewGuid()});
        _publisher.PublishToTopic<OrderCompletedEvent>(orderCompletedEvent, cancellationToken);
        return Task.CompletedTask;
    }
}
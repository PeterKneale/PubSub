using Amazon.SQS.Model;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PubSub.Subscribe;
using Samples.Messages;

namespace Samples.Subscriber;

public class Handler : ISubscriberMessageHandler
{
    private readonly ILogger<Handler> _log;

    public Handler(ILogger<Handler> log)
    {
        _log = log;
    }

    public Task Handle(Message submittedMessage, CancellationToken stoppingToken)
    {
        var orderCompletedEvent = JsonConvert.DeserializeObject<OrderCompletedEvent>(submittedMessage.Body);
        _log.LogInformation("*** Completed order {OrderId}", orderCompletedEvent.OrderId);
        return Task.CompletedTask;
    }
}
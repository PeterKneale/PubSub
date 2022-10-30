using Amazon.SQS.Model;
using Messages;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PubSub;

namespace Sub;

public class Handler : ISubMessageHandler
{
    private readonly ILogger<Handler> _log;

    public Handler(ILogger<Handler> log)
    {
        _log = log;
    }

    public Task Handle(Message message, CancellationToken stoppingToken)
    {
        var order = JsonConvert.DeserializeObject<OrderSubmittedEvent>(message.Body);
        _log.LogInformation("Received message {MessageBody} container order {OrderId}", message.Body, order.OrderId);
        return Task.CompletedTask;
    }
}
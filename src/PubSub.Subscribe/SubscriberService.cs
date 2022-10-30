using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace PubSub.Subscribe;

public class SubscriberService : BackgroundService
{
    private readonly ISubscriberConfiguration _config;
    private readonly ISubscriber _subscriber;
    private readonly IServiceProvider _provider;
    private readonly ILogger<SubscriberService> _log;
    private readonly TimeSpan _queueErrorDelay = TimeSpan.FromSeconds(5);
    private readonly TimeSpan _queueEmptyDelay = TimeSpan.FromSeconds(5);

    public SubscriberService(ISubscriberConfiguration config, ISubscriber subscriber, IServiceProvider provider, ILogger<SubscriberService> log)
    {
        _config = config;
        _subscriber = subscriber;
        _provider = provider;
        _log = log;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        _log.LogInformation("Background service starting");
        await _config.EnsureQueuesExist(CancellationToken.None);
        await base.StartAsync(cancellationToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _log.LogInformation("Background service stopping");
        await base.StopAsync(cancellationToken);
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessQueue(stoppingToken);
            }
            catch (TaskCanceledException)
            {
                _log.LogInformation("Background service canceled");
                break;
            }
            catch (Exception e)
            {
                _log.LogError(e, "Background service has encountered an error and will restart in {Seconds}", _queueErrorDelay.TotalSeconds);
                await Task.Delay(_queueErrorDelay, stoppingToken);
            }
        }
    }

    private async Task ProcessQueue(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var messages = await _subscriber.GetMessagesFromQueue(stoppingToken);
            if (messages.Any())
            {
                foreach (var message in messages)
                {
                    _log.LogInformation("Background service processing message {MessageId}", message.MessageId);
                    using var scope = _provider.CreateScope();
                    var handler = scope.ServiceProvider.GetRequiredService<ISubscriberMessageHandler>();

                    await handler.Handle(message, stoppingToken);
                    await _subscriber.DeleteMessageFromQueue(message.ReceiptHandle, stoppingToken);
                }
            }
            else
            {
                await Task.Delay(_queueEmptyDelay, stoppingToken);
            }
        }
    }
}
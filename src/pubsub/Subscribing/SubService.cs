using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace PubSub.Subscribing;

internal class SubService : BackgroundService
{
    private readonly IConfigureSub _configure;
    private readonly ISub _sub;
    private readonly IServiceProvider _provider;
    private readonly ILogger<SubService> _log;
    private readonly TimeSpan _queueErrorDelay = TimeSpan.FromSeconds(5);
    private readonly TimeSpan _queueEmptyDelay = TimeSpan.FromSeconds(5);
    private string _queueUrl = string.Empty;

    public SubService(IConfigureSub configure, ISub sub, IServiceProvider provider, ILogger<SubService> log)
    {
        _configure = configure;
        _sub = sub;
        _provider = provider;
        _log = log;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        _log.LogInformation("Background service starting");
        _queueUrl = await _configure.EnsureQueueExists(cancellationToken);

        await base.StartAsync(cancellationToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _log.LogInformation("Background service stopping");
        await base.StopAsync(cancellationToken);
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _log.LogInformation("Background service processing message from {QueueUrl}", _queueUrl);
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
            var messages = await _sub.GetMessagesFromQueue(stoppingToken);
            if (messages.Any())
            {
                foreach (var message in messages)
                {
                    _log.LogInformation("Background service processing message {MessageId}", message.MessageId);
                    using var scope = _provider.CreateScope();
                    var handler = scope.ServiceProvider.GetRequiredService<ISubMessageHandler>();

                    await handler.Handle(message, stoppingToken);
                    await _sub.DeleteMessageFromQueue(message.ReceiptHandle, stoppingToken);
                }
            }
            else
            {
                await Task.Delay(_queueEmptyDelay, stoppingToken);
            }
        }
    }
}
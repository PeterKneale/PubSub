using Amazon.SimpleNotificationService;
using Amazon.SQS;
using Messages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sub;

using var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services => {
        services
            .AddAWSService<IAmazonSQS>()
            .AddAWSService<IAmazonSimpleNotificationService>()
            .AddSub<Handler>()
            .AddLogging(c => {
                c.AddSimpleConsole(opt => opt.SingleLine = true);
            })
            .AddMemoryCache();
    })
    .Build();

await host.Services.ConfigureSub(async config => {
    await config.EnsureQueueExists(CancellationToken.None);
    await config.EnsureSubscriptionExists<OrderSubmittedEvent>(CancellationToken.None);
});

await host.RunAsync();
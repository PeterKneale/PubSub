using Amazon.SimpleNotificationService;
using Amazon.SQS;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Samples.Messages;
using Samples.PublisherAndSubscriber;

using var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services => {
        services
            .AddAWSService<IAmazonSQS>()
            .AddAWSService<IAmazonSimpleNotificationService>()
            .AddPublisher()
            .AddSubscriber<Handler>()
            .AddLogging(c => {
                c.AddSimpleConsole(opt => opt.SingleLine = true);
            })
            .AddMemoryCache();
    })
    .Build();

await host.Services.ConfigureSubscriber(async config => {
    await config.EnsureSubscriptionExists<OrderSubmittedEvent>(CancellationToken.None);
});
await host.Services.ConfigurePublisher(async config => {
    await config.EnsureTopicExists<OrderCompletedEvent>(CancellationToken.None);
});

await host.RunAsync();
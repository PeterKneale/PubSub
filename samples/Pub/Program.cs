using Amazon.SimpleNotificationService;
using Amazon.SQS;
using Messages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PubSub;

using var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services => {
        services
            .AddAWSService<IAmazonSQS>()
            .AddAWSService<IAmazonSimpleNotificationService>()
            .AddPub()
            .AddLogging(c => {
                c.AddSimpleConsole(opt => opt.SingleLine = true);
            });
    })
    .Build();


await host.Services.ConfigurePub(async config => {
    await config.EnsureTopicExists<OrderSubmittedEvent>(CancellationToken.None);
});

var publisher = host.Services.GetRequiredService<IPub>();
for (var i = 0; i < 100; i++)
{
    var json = JsonConvert.SerializeObject(new OrderSubmittedEvent {OrderId = Guid.NewGuid(), Amount = 1});
    Console.WriteLine($"Publishing message {i} {json}");
    await publisher.PublishToTopic<OrderSubmittedEvent>(json, CancellationToken.None);
}
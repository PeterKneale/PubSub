using Amazon.SimpleNotificationService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PubSub.Publish;
using Samples.Messages;

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

var provider = new ServiceCollection()
    .AddSingleton<IConfiguration>(configuration)
    .AddAWSService<IAmazonSimpleNotificationService>()
    .AddPublisher()
    .AddLogging(c => {
        c.AddSimpleConsole(opt => opt.SingleLine = true);
    })
    .AddMemoryCache()
    .BuildServiceProvider();

await provider.ConfigurePublisher(async config => {
    await config.EnsureTopicExists<OrderSubmittedEvent>(CancellationToken.None);
});

var publisher = provider.GetRequiredService<IPublisher>();

const int NumberToPublish = 1;

await Parallel.ForEachAsync(Enumerable.Range(0, NumberToPublish), async (index, cancellationToken) => {
    Console.WriteLine($"Publishing message {index}");
    await publisher.PublishToTopic<OrderSubmittedEvent>(BuildMessage(), cancellationToken);
});

string BuildMessage()
{
    return JsonConvert.SerializeObject(new OrderSubmittedEvent {OrderId = Guid.NewGuid()});
}
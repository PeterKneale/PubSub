using Amazon.SimpleNotificationService;
using Messages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PubSub.Publish;

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

// demo publish in series
for (var index = 0; index <= 100; index++)
{
    Console.WriteLine($"Publishing message {index}");
    var message = new OrderSubmittedEvent {OrderId = Guid.NewGuid(), Amount = index};
    var json = JsonConvert.SerializeObject(message);
    await publisher.PublishToTopic<OrderSubmittedEvent>(json, CancellationToken.None);
}

// demo publish in parallel
await Parallel.ForEachAsync(Enumerable.Range(0, 100), async (index, cancellationToken) => {
    Console.WriteLine($"Publishing message {index}");
    var message = new OrderSubmittedEvent {OrderId = Guid.NewGuid(), Amount = index};
    var json = JsonConvert.SerializeObject(message);
    await publisher.PublishToTopic<OrderSubmittedEvent>(json, cancellationToken);
});
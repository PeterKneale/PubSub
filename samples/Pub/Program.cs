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

await Parallel.ForEachAsync(Enumerable.Range(0, 100), async (index, cancellationToken) => {
    Console.WriteLine($"Publishing message {index}");
    var message = new OrderSubmittedEvent {OrderId = Guid.NewGuid(), Amount = 1};
    var json = JsonConvert.SerializeObject(message);
    await publisher.PublishToTopic<OrderSubmittedEvent>(json, cancellationToken);
});
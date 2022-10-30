using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace PubSub.Common.UnitTests;

public class QueueNameTests
{
    [Fact]
    public void Queue_name_is_correct()
    {
        var config = BuildConfig("au-dev", "discounts");
        var name = config.GetQueueName();
        name.Should().Be("au-dev-discounts");
    }
    
    [Fact]
    public void Dead_letter_queue_name_is_correct()
    {
        var config = BuildConfig("au-dev", "discounts");
        var name = config.GetDeadLetterQueueName();
        name.Should().Be("au-dev-discounts-dlq");
    }

    private static IConfiguration BuildConfig(string prefix, string service) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                {"pubsub:prefix", prefix},
                {"pubsub:service", service}
            })
            .Build();

}
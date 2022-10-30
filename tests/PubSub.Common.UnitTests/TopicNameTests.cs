using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace PubSub.Common.UnitTests;

public class TopicNameTests
{
    [Fact]
    public void Topic_name_is_correct()
    {
        var config = BuildConfig("au-dev");
        var name = config.GetTopicName<TestMessage>();
        name.Should().Be("au-dev-testmessage");
    }

    private static IConfiguration BuildConfig(string prefix) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                {"pubsub:prefix", prefix}
            })
            .Build();

    class TestMessage
    {

    }
}
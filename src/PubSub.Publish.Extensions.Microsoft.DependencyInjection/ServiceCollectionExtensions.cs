
using PubSub.Publish;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPublisher(this IServiceCollection services) =>
        services
            .AddTransient<IPublisher, Publisher>()
            .AddTransient<IPublisherConfiguration, PublisherConfiguration>();

    public static async Task ConfigurePublisher(this IServiceProvider provider, Func<IPublisherConfiguration, Task> configure) =>
        await configure(provider.GetRequiredService<IPublisherConfiguration>());
}
using PubSub.Subscribe;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSubscriber<THandler>(this IServiceCollection services) where THandler : class, ISubscriberMessageHandler =>
        services
            .AddTransient<ISubscriber, Subscriber>()
            .AddTransient<ISubscriberConfiguration, SubscriberConfiguration>()
            .AddTransient<ISubscriberMessageHandler, THandler>()
            .AddHostedService<SubscriberService>();

    public static async Task ConfigureSubscriber(this IServiceProvider provider, Func<ISubscriberConfiguration, Task> configure)
    {
        var config = provider.GetRequiredService<ISubscriberConfiguration>();
        await config.EnsureQueuesExist(CancellationToken.None);
        await configure(config);
    }
}
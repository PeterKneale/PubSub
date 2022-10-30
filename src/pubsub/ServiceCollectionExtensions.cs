using PubSub;
using PubSub.Publishing;
using PubSub.Subscribing;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPub(this IServiceCollection services) =>
        services
            .AddCommon()
            .AddTransient<IPub, Pub>()
            .AddTransient<IConfigurePub, ConfigurePub>();
    
    public static async Task ConfigurePub(this IServiceProvider provider, Func<IConfigurePub, Task> configure) => 
        await configure(provider.GetRequiredService<IConfigurePub>());

    public static IServiceCollection AddSub<THandler>(this IServiceCollection services) where THandler : class, ISubMessageHandler =>
        services
            .AddCommon()
            .AddTransient<ISub, Sub>()
            .AddTransient<IConfigureSub, ConfigureSub>()
            // Register the publisher interface but forward it to the bus implementation
            .AddTransient<ISubMessageHandler, THandler>()
            // Register the background service polling SQS
            .AddHostedService<SubService>();
    
    public static async Task ConfigureSub(this IServiceProvider provider, Func<IConfigureSub, Task> configure) => 
        await configure(provider.GetRequiredService<IConfigureSub>());
    private static IServiceCollection AddCommon(this IServiceCollection services) =>
        services
            .AddMemoryCache();
}
using AspNetAllocTracer;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAllocationTracing(this IServiceCollection services) => services.AddAllocationTracing(
        cfg => { });

    public static IServiceCollection AddAllocationTracing(this IServiceCollection services, Action<AllocTracerOptions> configure)
    {
        services.AddHostedService<AllocTracerService>();
        services.AddSingleton<AllocReporter>();
        services.AddOptions<AllocTracerOptions>().Configure(configure);
        return services;
    }
}
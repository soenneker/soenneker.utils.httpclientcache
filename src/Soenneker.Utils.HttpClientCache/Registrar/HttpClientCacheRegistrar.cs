using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soenneker.Utils.HttpClientCache.Abstract;

namespace Soenneker.Utils.HttpClientCache.Registrar;

/// <summary>
/// Registers the keyed HTTP client cache.
/// </summary>
public static class HttpClientCacheRegistrar
{
    /// <summary>
    /// Adds <see cref="HttpClientCache"/> as a singleton to the <see cref="IServiceCollection"/>
    /// </summary>
    /// <returns>Adds <see cref="HttpClientCache"/> as a singleton to the <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddHttpClientCacheAsSingleton(this IServiceCollection services)
    {
        services.TryAddSingleton<IHttpClientCache, HttpClientCache>();

        return services;
    }

    /// <summary>
    /// Registers the HTTP client cache and its dependencies with scoped lifetime.
    /// </summary>
    /// <param name="services">The service collection to resolve or update.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddHttpClientCacheAsScoped(this IServiceCollection services)
    {
        services.TryAddScoped<IHttpClientCache, HttpClientCache>();

        return services;
    }
}

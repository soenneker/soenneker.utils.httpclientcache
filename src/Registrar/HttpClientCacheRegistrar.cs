using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soenneker.Utils.HttpClientCache.Abstract;

namespace Soenneker.Utils.HttpClientCache.Registrar;

/// <summary>
///  A utility library for singleton thread-safe HttpClients
/// </summary>
public static class HttpClientCacheRegistrar
{
    /// <summary>
    /// Adds <see cref="HttpClientCache"/>
    /// </summary>
    public static void AddHttpClientCacheAsSingleton(this IServiceCollection services)
    {
        services.TryAddSingleton<IHttpClientCache, HttpClientCache>();
    }
}
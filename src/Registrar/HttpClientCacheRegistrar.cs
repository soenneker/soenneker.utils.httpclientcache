﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soenneker.Utils.HttpClientCache.Abstract;

namespace Soenneker.Utils.HttpClientCache.Registrar;

/// <summary>
///  A utility library for singleton thread-safe HttpClients
/// </summary>
public static class HttpClientCacheRegistrar
{
    /// <summary>
    /// Adds <see cref="HttpClientCache"/> as a singleton to the <see cref="IServiceCollection"/>
    /// </summary>
    public static IServiceCollection AddHttpClientCacheAsSingleton(this IServiceCollection services)
    {
        services.TryAddSingleton<IHttpClientCache, HttpClientCache>();

        return services;
    }

    public static IServiceCollection AddHttpClientCacheAsScoped(this IServiceCollection services)
    {
        services.TryAddScoped<IHttpClientCache, HttpClientCache>();

        return services;
    }
}
using Soenneker.Dtos.HttpClientOptions;
using Soenneker.Extensions.Enumerable;
using Soenneker.Extensions.ValueTask;
using Soenneker.Utils.HttpClientCache.Abstract;
using Soenneker.Utils.Runtime;
using Soenneker.Utils.SingletonDictionary;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Utils.HttpClientCache;

///<inheritdoc cref="IHttpClientCache"/>
public sealed class HttpClientCache : IHttpClientCache
{
    private readonly SingletonDictionary<HttpClient, Func<CancellationToken, ValueTask<HttpClientOptions?>>> _httpClients;
    private readonly ConcurrentDictionary<HandlerKey, SocketsHttpHandler> _handlers = new();

    private static readonly Func<CancellationToken, ValueTask<HttpClientOptions?>> _nullOptionsFactory = static _ => default;

    public HttpClientCache()
    {
        // We need the token-aware init path so we can call optionsFactory(token).
        _httpClients =
            new SingletonDictionary<HttpClient, Func<CancellationToken, ValueTask<HttpClientOptions?>>>(async (_, cancellationToken, optionsFactory) =>
            {
                HttpClientOptions? options = await optionsFactory(cancellationToken)
                    .NoSync();

                HttpClient httpClient = CreateHttpClient(options);

                await ConfigureHttpClient(httpClient, options)
                    .NoSync();

                return httpClient;
            });
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<HttpClient> Get(string id, CancellationToken cancellationToken = default) =>
        _httpClients.Get(id, () => _nullOptionsFactory, cancellationToken);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<HttpClient> Get(string id, Func<CancellationToken, ValueTask<HttpClientOptions?>> optionsFactory,
        CancellationToken cancellationToken = default) => _httpClients.Get(id, () => optionsFactory, cancellationToken);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<HttpClient> Get(string id, Func<HttpClientOptions?> optionsFactory, CancellationToken cancellationToken = default) =>
        _httpClients.Get(id, () => _ => new ValueTask<HttpClientOptions?>(optionsFactory()), cancellationToken);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<HttpClient> Get(string id, Func<ValueTask<HttpClientOptions?>> optionsFactory, CancellationToken cancellationToken = default) =>
        _httpClients.Get(id, () => _ => optionsFactory(), cancellationToken);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HttpClient GetSync(string id, CancellationToken cancellationToken = default) =>
        _httpClients.GetSync(id, () => _nullOptionsFactory, cancellationToken);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HttpClient GetSync(
        string id, Func<CancellationToken, ValueTask<HttpClientOptions?>> optionsFactory, CancellationToken cancellationToken = default) =>
        _httpClients.GetSync(id, () => optionsFactory, cancellationToken);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HttpClient GetSync(string id, Func<HttpClientOptions?> optionsFactory, CancellationToken cancellationToken = default) =>
        _httpClients.GetSync(id, () => _ => new ValueTask<HttpClientOptions?>(optionsFactory()), cancellationToken);

    private HttpClient CreateHttpClient(HttpClientOptions? options)
    {
        if (RuntimeUtil.IsBrowser())
        {
            return options?.HttpClientHandler != null ? new HttpClient(options.HttpClientHandler, disposeHandler: false) : new HttpClient();
        }

        return options?.HttpClientHandler != null
            ? new HttpClient(options.HttpClientHandler, disposeHandler: false)
            : new HttpClient(GetOrCreateHandler(options), disposeHandler: false);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static async ValueTask ConfigureHttpClient(HttpClient httpClient, HttpClientOptions? options)
    {
        httpClient.Timeout = options?.Timeout ?? TimeSpan.FromSeconds(100);

        if (options?.BaseAddress != null)
        {
            Uri baseUri = new(options.BaseAddress);

            if (!Equals(httpClient.BaseAddress, baseUri))
                httpClient.BaseAddress = baseUri;
        }

        AddDefaultRequestHeaders(httpClient, options?.DefaultRequestHeaders);

        Func<HttpClient, ValueTask>? modifyClient = options?.ModifyClient;

        if (modifyClient is not null)
            await modifyClient(httpClient)
                .NoSync();
    }

    private SocketsHttpHandler GetOrCreateHandler(HttpClientOptions? options)
    {
        double connectTimeoutSeconds = options?.Timeout?.TotalSeconds ?? 100;
        
        var key = new HandlerKey(
            LifetimeSeconds: options?.PooledConnectionLifetime?.TotalSeconds ?? 600,
            MaxConnections: options?.MaxConnectionsPerServer ?? 40,
            UseCookies: options?.UseCookieContainer == true,
            ConnectTimeoutSeconds: connectTimeoutSeconds,
            ResponseDrainTimeoutSeconds: options?.ResponseDrainTimeout?.TotalSeconds,
            AllowAutoRedirect: options?.AllowAutoRedirect,
            AutomaticDecompression: (int?)options?.AutomaticDecompression,
            KeepAlivePingDelaySeconds: options?.KeepAlivePingDelay?.TotalSeconds,
            KeepAlivePingTimeoutSeconds: options?.KeepAlivePingTimeout?.TotalSeconds,
            KeepAlivePingPolicy: (int?)options?.KeepAlivePingPolicy,
            UseProxy: options?.UseProxy,
            ProxyHashCode: options?.Proxy?.GetHashCode(),
            MaxResponseDrainSize: options?.MaxResponseDrainSize,
            MaxResponseHeadersLength: options?.MaxResponseHeadersLength,
            SslOptionsHashCode: options?.SslOptions?.GetHashCode());

        return _handlers.GetOrAdd(key, _ =>
        {
            var handler = new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromSeconds(key.LifetimeSeconds),
                MaxConnectionsPerServer = key.MaxConnections,
                ConnectTimeout = TimeSpan.FromSeconds(key.ConnectTimeoutSeconds)
            };

            if (key.UseCookies)
                handler.CookieContainer = new CookieContainer();

            if (key.ResponseDrainTimeoutSeconds.HasValue)
                handler.ResponseDrainTimeout = TimeSpan.FromSeconds(key.ResponseDrainTimeoutSeconds.Value);

            if (key.AllowAutoRedirect.HasValue)
                handler.AllowAutoRedirect = key.AllowAutoRedirect.Value;

            if (key.AutomaticDecompression.HasValue)
                handler.AutomaticDecompression = (DecompressionMethods)key.AutomaticDecompression.Value;

            if (key.KeepAlivePingDelaySeconds.HasValue)
                handler.KeepAlivePingDelay = TimeSpan.FromSeconds(key.KeepAlivePingDelaySeconds.Value);

            if (key.KeepAlivePingTimeoutSeconds.HasValue)
                handler.KeepAlivePingTimeout = TimeSpan.FromSeconds(key.KeepAlivePingTimeoutSeconds.Value);

            if (key.KeepAlivePingPolicy.HasValue)
                handler.KeepAlivePingPolicy = (HttpKeepAlivePingPolicy)key.KeepAlivePingPolicy.Value;

            if (key.UseProxy.HasValue)
                handler.UseProxy = key.UseProxy.Value;

            if (options?.Proxy != null)
                handler.Proxy = options.Proxy;

            if (key.MaxResponseDrainSize.HasValue)
                handler.MaxResponseDrainSize = key.MaxResponseDrainSize.Value;

            if (key.MaxResponseHeadersLength.HasValue)
                handler.MaxResponseHeadersLength = key.MaxResponseHeadersLength.Value;

            if (options?.SslOptions != null)
                handler.SslOptions = options.SslOptions;

            return handler;
        });
    }

    private static void AddDefaultRequestHeaders(HttpClient httpClient, Dictionary<string, string>? headers)
    {
        if (headers.IsNullOrEmpty())
            return;

        foreach (KeyValuePair<string, string> header in headers)
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
    }

    public ValueTask Remove(string id, CancellationToken cancellationToken = default) => _httpClients.Remove(id, cancellationToken);

    public void RemoveSync(string id, CancellationToken cancellationToken = default) => _httpClients.RemoveSync(id, cancellationToken);

    private void DisposeHandlers()
    {
        foreach (KeyValuePair<HandlerKey, SocketsHttpHandler> kvp in _handlers.ToArray())
        {
            if (_handlers.TryRemove(kvp.Key, out SocketsHttpHandler? handler))
                handler.Dispose();
        }
    }

    public ValueTask DisposeAsync()
    {
        DisposeHandlers();
        return _httpClients.DisposeAsync();
    }

    public void Dispose()
    {
        DisposeHandlers();
        _httpClients.Dispose();
    }
}
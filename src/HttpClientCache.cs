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
using System.Net.Security;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Utils.HttpClientCache;

///<inheritdoc cref="IHttpClientCache"/>
public sealed class HttpClientCache : IHttpClientCache
{
    private readonly SingletonDictionary<HttpClient, OptionsFactory> _httpClients;
    private readonly ConcurrentDictionary<HandlerKey, SocketsHttpHandler> _handlers = new();

    private static readonly bool _isBrowser = RuntimeUtil.IsBrowser();

    private static readonly TimeSpan _defaultTimeout = TimeSpan.FromSeconds(100);
    private static readonly TimeSpan _defaultConnectTimeout = TimeSpan.FromSeconds(100);
    private static readonly TimeSpan _defaultPooledLifetime = TimeSpan.FromMinutes(10);

    public HttpClientCache()
    {
        // Use method group to avoid a closure and still access instance state.
        _httpClients = new SingletonDictionary<HttpClient, OptionsFactory>(InitializeHttpClient);
    }

    private async ValueTask<HttpClient> InitializeHttpClient(string _, CancellationToken cancellationToken, OptionsFactory factory)
    {
        HttpClientOptions? options = await factory.Invoke(cancellationToken)
                                                  .NoSync();

        HttpClient httpClient = CreateHttpClient(options);

        await ConfigureHttpClient(httpClient, options)
            .NoSync();

        return httpClient;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<HttpClient> Get(string id, CancellationToken cancellationToken = default) =>
        _httpClients.Get(id, OptionsFactory.Null, cancellationToken);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<HttpClient> Get(string id, Func<CancellationToken, ValueTask<HttpClientOptions?>> optionsFactory,
        CancellationToken cancellationToken = default) =>
        _httpClients.Get(id, OptionsFactory.From(optionsFactory), cancellationToken);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<HttpClient> Get(string id, Func<HttpClientOptions?> optionsFactory, CancellationToken cancellationToken = default) =>
        _httpClients.Get(id, OptionsFactory.From(optionsFactory), cancellationToken);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<HttpClient> Get(string id, Func<ValueTask<HttpClientOptions?>> optionsFactory, CancellationToken cancellationToken = default) =>
        _httpClients.Get(id, OptionsFactory.From(optionsFactory), cancellationToken);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HttpClient GetSync(string id, CancellationToken cancellationToken = default) =>
        _httpClients.GetSync(id, OptionsFactory.Null, cancellationToken);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HttpClient GetSync(string id, Func<CancellationToken, ValueTask<HttpClientOptions?>> optionsFactory,
        CancellationToken cancellationToken = default) =>
        _httpClients.GetSync(id, OptionsFactory.From(optionsFactory), cancellationToken);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HttpClient GetSync(string id, Func<HttpClientOptions?> optionsFactory, CancellationToken cancellationToken = default) =>
        _httpClients.GetSync(id, OptionsFactory.From(optionsFactory), cancellationToken);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HttpClient GetSync(string id, Func<ValueTask<HttpClientOptions?>> optionsFactory, CancellationToken cancellationToken = default) =>
        _httpClients.GetSync(id, OptionsFactory.From(optionsFactory), cancellationToken);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private HttpClient CreateHttpClient(HttpClientOptions? options)
    {
        if (_isBrowser)
        {
            return options?.HttpClientHandler != null ? new HttpClient(options.HttpClientHandler, disposeHandler: false) : new HttpClient();
        }

        return options?.HttpClientHandler != null
            ? new HttpClient(options.HttpClientHandler, disposeHandler: false)
            : new HttpClient(GetOrCreateHandler(options), disposeHandler: false);
    }

    // Remove async state machine when ModifyClient is null.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ValueTask ConfigureHttpClient(HttpClient httpClient, HttpClientOptions? options)
    {
        httpClient.Timeout = options?.Timeout ?? _defaultTimeout;

        // Prefer Uri to avoid parsing/allocation.
        Uri? baseUri = options?.BaseAddressUri;

        if (baseUri is not null && !Equals(httpClient.BaseAddress, baseUri))
            httpClient.BaseAddress = baseUri;

        AddDefaultRequestHeaders(httpClient, options?.DefaultRequestHeaders);

        Func<HttpClient, ValueTask>? modifyClient = options?.ModifyClient;
        return modifyClient?.Invoke(httpClient) ?? default;
    }

    private SocketsHttpHandler GetOrCreateHandler(HttpClientOptions? options)
    {
        // Do NOT tie connect timeout to request timeout.
        TimeSpan connectTimeout = options?.ConnectTimeout ?? _defaultConnectTimeout;

        // Extract refs so they become part of the key (no closure, no hash-key risk)
        IWebProxy? proxy = options?.Proxy;
        SslClientAuthenticationOptions? sslOptions = options?.SslOptions;

        var key = new HandlerKey(PooledConnectionLifetimeTicks: (options?.PooledConnectionLifetime ?? _defaultPooledLifetime).Ticks,
            MaxConnectionsPerServer: options?.MaxConnectionsPerServer ?? 40, UseCookies: options?.UseCookieContainer == true,
            ConnectTimeoutTicks: connectTimeout.Ticks, ResponseDrainTimeoutTicks: options?.ResponseDrainTimeout?.Ticks,
            AllowAutoRedirect: options?.AllowAutoRedirect, AutomaticDecompression: options?.AutomaticDecompression,
            KeepAlivePingDelayTicks: options?.KeepAlivePingDelay?.Ticks, KeepAlivePingTimeoutTicks: options?.KeepAlivePingTimeout?.Ticks,
            KeepAlivePingPolicy: options?.KeepAlivePingPolicy, UseProxy: options?.UseProxy, Proxy: proxy, MaxResponseDrainSize: options?.MaxResponseDrainSize,
            MaxResponseHeadersLength: options?.MaxResponseHeadersLength, SslOptions: sslOptions);

        // static factory => no closure allocation
        return _handlers.GetOrAdd(key, static k => CreateHandlerFromKey(k));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static SocketsHttpHandler CreateHandlerFromKey(in HandlerKey key)
    {
        var handler = new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromTicks(key.PooledConnectionLifetimeTicks),
            MaxConnectionsPerServer = key.MaxConnectionsPerServer,
            ConnectTimeout = TimeSpan.FromTicks(key.ConnectTimeoutTicks)
        };

        if (key.UseCookies)
            handler.CookieContainer = new CookieContainer();

        if (key.ResponseDrainTimeoutTicks.HasValue)
            handler.ResponseDrainTimeout = TimeSpan.FromTicks(key.ResponseDrainTimeoutTicks.Value);

        if (key.AllowAutoRedirect.HasValue)
            handler.AllowAutoRedirect = key.AllowAutoRedirect.Value;

        if (key.AutomaticDecompression.HasValue)
            handler.AutomaticDecompression = key.AutomaticDecompression.Value;

        if (key.KeepAlivePingDelayTicks.HasValue)
            handler.KeepAlivePingDelay = TimeSpan.FromTicks(key.KeepAlivePingDelayTicks.Value);

        if (key.KeepAlivePingTimeoutTicks.HasValue)
            handler.KeepAlivePingTimeout = TimeSpan.FromTicks(key.KeepAlivePingTimeoutTicks.Value);

        if (key.KeepAlivePingPolicy.HasValue)
            handler.KeepAlivePingPolicy = key.KeepAlivePingPolicy.Value;

        if (key.UseProxy.HasValue)
            handler.UseProxy = key.UseProxy.Value;

        if (key.Proxy is not null)
            handler.Proxy = key.Proxy;

        if (key.MaxResponseDrainSize.HasValue)
            handler.MaxResponseDrainSize = key.MaxResponseDrainSize.Value;

        if (key.MaxResponseHeadersLength.HasValue)
            handler.MaxResponseHeadersLength = key.MaxResponseHeadersLength.Value;

        if (key.SslOptions is not null)
            handler.SslOptions = key.SslOptions;

        return handler;
    }

    private static void AddDefaultRequestHeaders(HttpClient httpClient, Dictionary<string, string>? headers)
    {
        if (headers.IsNullOrEmpty())
            return;

        foreach (KeyValuePair<string, string> header in headers)
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
    }

    public ValueTask Remove(string id, CancellationToken cancellationToken = default) =>
        _httpClients.Remove(id, cancellationToken);

    public void RemoveSync(string id, CancellationToken cancellationToken = default) =>
        _httpClients.RemoveSync(id, cancellationToken);

    private void DisposeHandlers()
    {
        foreach (KeyValuePair<HandlerKey, SocketsHttpHandler> kvp in _handlers)
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
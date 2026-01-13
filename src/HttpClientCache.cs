using Soenneker.Dtos.HttpClientOptions;
using Soenneker.Extensions.Enumerable;
using Soenneker.Extensions.ValueTask;
using Soenneker.Dictionaries.SingletonKeys;
using Soenneker.Utils.HttpClientCache.Abstract;
using Soenneker.Utils.Runtime;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Soenneker.Dictionaries.Singletons;

namespace Soenneker.Utils.HttpClientCache;

///<inheritdoc cref="IHttpClientCache"/>
public sealed class HttpClientCache : IHttpClientCache
{
    private readonly SingletonDictionary<HttpClient, OptionsFactory> _httpClients;
    private readonly SingletonKeyDictionary<HandlerKey, SocketsHttpHandler> _handlers;

    private static readonly bool _isBrowser = RuntimeUtil.IsBrowser();

    private static readonly TimeSpan _defaultTimeout = TimeSpan.FromSeconds(100);
    private static readonly TimeSpan _defaultConnectTimeout = TimeSpan.FromSeconds(100);
    private static readonly TimeSpan _defaultPooledLifetime = TimeSpan.FromMinutes(10);

    public HttpClientCache()
    {
        // Use method group to avoid a closure and still access instance state.
        _httpClients = new SingletonDictionary<HttpClient, OptionsFactory>(InitializeHttpClient);

        // Handlers are expensive and should be reused; use a keyed singleton dictionary to guarantee a single handler per key.
        _handlers = new SingletonKeyDictionary<HandlerKey, SocketsHttpHandler>(static k => CreateHandlerFromKey(in k));
    }

    private async ValueTask<HttpClient> InitializeHttpClient(string _, OptionsFactory factory, CancellationToken cancellationToken)
    {
        // Maintain sync context
        HttpClientOptions? options = await factory.Invoke(cancellationToken);

        HttpClient httpClient = CreateHttpClient(options);

        await ConfigureHttpClient(httpClient, options);

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
    public ValueTask<HttpClient> Get<TState>(string id, TState state, Func<TState, HttpClientOptions?> optionsFactory,
        CancellationToken cancellationToken = default) where TState : notnull =>
        _httpClients.Get(id, (state, optionsFactory), static s => OptionsFactory.From(s.state, s.optionsFactory), cancellationToken);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<HttpClient> Get<TState>(string id, TState state, Func<TState, ValueTask<HttpClientOptions?>> optionsFactory,
        CancellationToken cancellationToken = default) where TState : notnull =>
        _httpClients.Get(id, (state, optionsFactory), static s => OptionsFactory.From(s.state, s.optionsFactory), cancellationToken);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<HttpClient> Get<TState>(string id, TState state, Func<TState, CancellationToken, ValueTask<HttpClientOptions?>> optionsFactory,
        CancellationToken cancellationToken = default) where TState : notnull =>
        _httpClients.Get(id, (state, optionsFactory), static s => OptionsFactory.From(s.state, s.optionsFactory), cancellationToken);

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

        if (options?.HttpClientHandler != null)
            return new HttpClient(options.HttpClientHandler, disposeHandler: false);

        // If caller supplies per-client proxy/SSL options, do NOT put those into the shared handler cache key
        // (it’s a common source of unbounded handler growth when options instances are created per call).
        // Instead, create a dedicated handler and attach it to the HttpClient so it will be disposed when the client is disposed.
        if (options?.Proxy is not null || options?.SslOptions is not null)
            return new HttpClient(CreateHandler(options), disposeHandler: true);

        return new HttpClient(GetOrCreateHandler(options), disposeHandler: false);
    }

    // Remove async state machine when ModifyClient is null.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ValueTask ConfigureHttpClient(HttpClient httpClient, HttpClientOptions? options)
    {
        httpClient.Timeout = options?.Timeout ?? _defaultTimeout;

        // Prefer Uri to avoid parsing/allocation.
        Uri? baseUri = options?.BaseAddress;

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

        var key = new HandlerKey(PooledConnectionLifetimeTicks: (options?.PooledConnectionLifetime ?? _defaultPooledLifetime).Ticks,
            MaxConnectionsPerServer: options?.MaxConnectionsPerServer ?? 40, UseCookies: options?.UseCookieContainer == true,
            ConnectTimeoutTicks: connectTimeout.Ticks, ResponseDrainTimeoutTicks: options?.ResponseDrainTimeout?.Ticks,
            AllowAutoRedirect: options?.AllowAutoRedirect, AutomaticDecompression: options?.AutomaticDecompression,
            KeepAlivePingDelayTicks: options?.KeepAlivePingDelay?.Ticks, KeepAlivePingTimeoutTicks: options?.KeepAlivePingTimeout?.Ticks,
            KeepAlivePingPolicy: options?.KeepAlivePingPolicy, UseProxy: options?.UseProxy, MaxResponseDrainSize: options?.MaxResponseDrainSize,
            MaxResponseHeadersLength: options?.MaxResponseHeadersLength);

        return _handlers.GetSync(key);
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

        if (key.MaxResponseDrainSize.HasValue)
            handler.MaxResponseDrainSize = key.MaxResponseDrainSize.Value;

        if (key.MaxResponseHeadersLength.HasValue)
            handler.MaxResponseHeadersLength = key.MaxResponseHeadersLength.Value;

        return handler;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static SocketsHttpHandler CreateHandler(HttpClientOptions? options)
    {
        // Dedicated handler for per-client settings (proxy/SSL). Keep it consistent with CreateHandlerFromKey defaults.
        var handler = new SocketsHttpHandler
        {
            PooledConnectionLifetime = options?.PooledConnectionLifetime ?? _defaultPooledLifetime,
            MaxConnectionsPerServer = options?.MaxConnectionsPerServer ?? 40,
            ConnectTimeout = options?.ConnectTimeout ?? _defaultConnectTimeout
        };

        if (options?.UseCookieContainer == true)
            handler.CookieContainer = new CookieContainer();

        if (options?.ResponseDrainTimeout is { } responseDrainTimeout)
            handler.ResponseDrainTimeout = responseDrainTimeout;

        if (options?.AllowAutoRedirect is { } allowAutoRedirect)
            handler.AllowAutoRedirect = allowAutoRedirect;

        if (options?.AutomaticDecompression is { } decompression)
            handler.AutomaticDecompression = decompression;

        if (options?.KeepAlivePingDelay is { } keepAlivePingDelay)
            handler.KeepAlivePingDelay = keepAlivePingDelay;

        if (options?.KeepAlivePingTimeout is { } keepAlivePingTimeout)
            handler.KeepAlivePingTimeout = keepAlivePingTimeout;

        if (options?.KeepAlivePingPolicy is { } keepAlivePingPolicy)
            handler.KeepAlivePingPolicy = keepAlivePingPolicy;

        if (options?.UseProxy is { } useProxy)
            handler.UseProxy = useProxy;

        if (options?.Proxy is not null)
            handler.Proxy = options.Proxy;

        if (options?.MaxResponseDrainSize is { } maxResponseDrainSize)
            handler.MaxResponseDrainSize = maxResponseDrainSize;

        if (options?.MaxResponseHeadersLength is { } maxResponseHeadersLength)
            handler.MaxResponseHeadersLength = maxResponseHeadersLength;

        if (options?.SslOptions is not null)
            handler.SslOptions = options.SslOptions;

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

    public async ValueTask DisposeAsync()
    {
        await _handlers.DisposeAsync();
        await _httpClients.DisposeAsync();
    }

    public void Dispose()
    {
        _handlers.Dispose();
        _httpClients.Dispose();
    }
}
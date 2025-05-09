using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Soenneker.Dtos.HttpClientOptions;
using Soenneker.Extensions.ValueTask;
using Soenneker.Utils.HttpClientCache.Abstract;
using Soenneker.Utils.Runtime;
using Soenneker.Utils.SingletonDictionary;

namespace Soenneker.Utils.HttpClientCache;

///<inheritdoc cref="IHttpClientCache"/>
public class HttpClientCache : IHttpClientCache
{
    private readonly SingletonDictionary<HttpClient> _httpClients;

    public HttpClientCache()
    {
        _httpClients = new SingletonDictionary<HttpClient>(async args =>
        {
            HttpClientOptions? options = null;

            if (args.Length > 0)
            {
                object arg = args[0];

                if (arg is Func<ValueTask<HttpClientOptions?>> asyncFactory)
                    options = await asyncFactory().NoSync();
                else if (arg is Func<HttpClientOptions?> syncFactory)
                    options = syncFactory();
                else if (arg is HttpClientOptions directOptions)
                    options = directOptions;
            }

            HttpClient httpClient = CreateHttpClient(options);

            await ConfigureHttpClient(httpClient, options).NoSync();

            return httpClient;
        });
    }

    private static HttpClient CreateHttpClient(HttpClientOptions? options)
    {
        if (RuntimeUtil.IsBrowser())
        {
            return options?.HttpClientHandler != null ? new HttpClient(options.HttpClientHandler) : new HttpClient();
        }

        return options?.HttpClientHandler != null ? new HttpClient(options.HttpClientHandler) : new HttpClient(CreateSocketsHttpHandler(options));
    }

    public ValueTask<HttpClient> Get(string id, HttpClientOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (options == null)
            return _httpClients.Get(id, cancellationToken);

        return _httpClients.Get(id, cancellationToken, options);
    }

    public ValueTask<HttpClient> Get(string id, Func<HttpClientOptions?> optionsFactory, CancellationToken cancellationToken = default)
    {
        return _httpClients.Get(id, cancellationToken, optionsFactory);
    }

    public ValueTask<HttpClient> Get(string id, Func<ValueTask<HttpClientOptions?>> optionsFactory, CancellationToken cancellationToken = default)
    {
        return _httpClients.Get(id, cancellationToken, optionsFactory);
    }

    public HttpClient GetSync(string id, HttpClientOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (options == null)
            return _httpClients.GetSync(id, cancellationToken);

        return _httpClients.GetSync(id, cancellationToken, options);
    }

    public HttpClient GetSync(string id, Func<HttpClientOptions?> optionsFactory, CancellationToken cancellationToken = default)
    {
        HttpClientOptions? options = optionsFactory.Invoke();
        return _httpClients.GetSync(id, cancellationToken, options);
    }

    private static async ValueTask ConfigureHttpClient(HttpClient httpClient, HttpClientOptions? options)
    {
        httpClient.Timeout = options?.Timeout ?? TimeSpan.FromSeconds(100);

        if (options?.BaseAddress != null)
            httpClient.BaseAddress = new Uri(options.BaseAddress);

        AddDefaultRequestHeaders(httpClient, options?.DefaultRequestHeaders);

        if (options?.ModifyClient != null)
            await options.ModifyClient.Invoke(httpClient).NoSync();
    }

    private static SocketsHttpHandler CreateSocketsHttpHandler(HttpClientOptions? options)
    {
        var handler = new SocketsHttpHandler
        {
            PooledConnectionLifetime = options?.PooledConnectionLifetime ?? TimeSpan.FromMinutes(10),
            MaxConnectionsPerServer = options?.MaxConnectionsPerServer ?? 40
        };

        if (options?.UseCookieContainer == true)
        {
            handler.CookieContainer = new CookieContainer();
        }

        return handler;
    }

    private static void AddDefaultRequestHeaders(HttpClient httpClient, Dictionary<string, string>? headers)
    {
        if (headers == null)
            return;

        foreach (KeyValuePair<string, string> header in headers)
        {
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
        }
    }

    public ValueTask Remove(string id)
    {
        return _httpClients.Remove(id);
    }

    public void RemoveSync(string id)
    {
        _httpClients.RemoveSync(id);
    }

    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return _httpClients.DisposeAsync();
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _httpClients.Dispose();
    }
}
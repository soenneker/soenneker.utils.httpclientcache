using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Soenneker.Extensions.ValueTask;
using Soenneker.Utils.HttpClientCache.Abstract;
using Soenneker.Utils.HttpClientCache.Dtos;
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
            var options = args.FirstOrDefault() as HttpClientOptions;
            HttpClient httpClient = CreateHttpClient(options);

            await ConfigureHttpClient(httpClient, options).NoSync();

            return httpClient;
        });
    }

    private static HttpClient CreateHttpClient(HttpClientOptions? options)
    {
        if (RuntimeUtil.IsBrowser())
        {
            return options?.HttpClientHandler != null
                ? new HttpClient(options.HttpClientHandler)
                : new HttpClient();
        }

        return options?.HttpClientHandler != null
            ? new HttpClient(options.HttpClientHandler)
            : new HttpClient(CreateSocketsHttpHandler(options));
    }

    public ValueTask<HttpClient> Get(string id, HttpClientOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (options == null)
            return _httpClients.Get(id, cancellationToken);

        return _httpClients.Get(id, cancellationToken, options);
    }

    public HttpClient GetSync(string id, HttpClientOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (options == null)
            return _httpClients.GetSync(id, cancellationToken);

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
        var handler = new SocketsHttpHandler();

        handler.PooledConnectionLifetime = options?.PooledConnectionLifetime ?? TimeSpan.FromMinutes(10);
        handler.MaxConnectionsPerServer = options?.MaxConnectionsPerServer ?? 40;

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
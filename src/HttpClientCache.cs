using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Soenneker.Extensions.Enumerable;
using Soenneker.Utils.HttpClientCache.Abstract;
using Soenneker.Utils.SingletonDictionary;

namespace Soenneker.Utils.HttpClientCache;

///<inheritdoc cref="IHttpClientCache"/>
public class HttpClientCache : IHttpClientCache
{
    private readonly SingletonDictionary<HttpClient> _httpClients;

    public HttpClientCache()
    {
        _httpClients = new SingletonDictionary<HttpClient>(args =>
        {
            var socketsHandler = new SocketsHttpHandler();

            Dictionary<string, object>? argsDict = null;

            if (args.IsNullOrEmpty())
            {
                socketsHandler.PooledConnectionLifetime = TimeSpan.FromMinutes(10);
                socketsHandler.MaxConnectionsPerServer = 40;
            }
            else
            {
                argsDict = (Dictionary<string, object>) args.First();

                socketsHandler.PooledConnectionLifetime = GetPooledConnectionLifetime(argsDict);
                
                if (GetCookieContainer(argsDict))
                    socketsHandler.CookieContainer = new CookieContainer();

                socketsHandler.MaxConnectionsPerServer = GetMaxConnectionsPerServer(argsDict);
            }

            var httpClient = new HttpClient(socketsHandler);

            if (argsDict != null && argsDict.TryGetValue("timeout", out object? value))
            {
                httpClient.Timeout = (TimeSpan) value;
            }

            return httpClient;
        });
    }

    private static int GetMaxConnectionsPerServer(Dictionary<string, object> argsDict)
    {
        if (argsDict.TryGetValue("maxConnectionsPerServer", out object? value))
        {
            if (value is int maxConnectionsPerServer)
                return maxConnectionsPerServer;
        }

        return 40;
    }

    private static bool GetCookieContainer(Dictionary<string, object> args)
    {
        if (args.TryGetValue("cookieContainer", out object? cookieContainerObj))
        {
            if (cookieContainerObj is bool cookieContainer)
                return cookieContainer;
        }

        return false;
    }

    private static TimeSpan GetPooledConnectionLifetime(Dictionary<string, object> args)
    {
        if (args.TryGetValue("pooledConnectionLifetime", out object? timeSpanObj))
        {
            if (timeSpanObj is TimeSpan timeSpan)
                return timeSpan;
        }

        return TimeSpan.FromMinutes(10);
    }

    public ValueTask<HttpClient> Get(string id, TimeSpan? pooledConnectionLifetime = null, bool? cookieContainer = null,
        int? maxConnectionsPerServer = null, TimeSpan? timeout = null)
    {
        if (NoConfigIsSet(pooledConnectionLifetime, cookieContainer, maxConnectionsPerServer, timeout))
            return _httpClients.Get(id);

        Dictionary<string, object> args = GetArgsDict(pooledConnectionLifetime, cookieContainer, maxConnectionsPerServer, timeout);

        return _httpClients.Get(id, args);
    }

    public HttpClient GetSync(string id, TimeSpan? pooledConnectionLifetime = null, bool? cookieContainer = null,
        int? maxConnectionsPerServer = null, TimeSpan? timeout = null)
    {
        if (NoConfigIsSet(pooledConnectionLifetime, cookieContainer, maxConnectionsPerServer, timeout))
            return _httpClients.GetSync(id);

        Dictionary<string, object> args = GetArgsDict(pooledConnectionLifetime, cookieContainer, maxConnectionsPerServer, timeout);

        return _httpClients.GetSync(id, args);
    }

    private static bool NoConfigIsSet(TimeSpan? pooledConnectionLifetime = null, bool? cookieContainer = null,
        int? maxConnectionsPerServer = null, TimeSpan? timeout = null)
    {
        return pooledConnectionLifetime == null && cookieContainer == null && maxConnectionsPerServer == null && timeout == null;
    }

    private static Dictionary<string, object> GetArgsDict(TimeSpan? pooledConnectionLifetime = null, bool? cookieContainer = null,
        int? maxConnectionsPerServer = null, TimeSpan? timeout = null)
    {
        var args = new Dictionary<string, object>();

        if (pooledConnectionLifetime != null)
            args.Add(nameof(pooledConnectionLifetime), pooledConnectionLifetime);

        if (cookieContainer != null)
            args.Add(nameof(cookieContainer), cookieContainer);

        if (maxConnectionsPerServer != null)
            args.Add(nameof(maxConnectionsPerServer), maxConnectionsPerServer);

        if (timeout != null)
            args.Add(nameof(timeout), timeout);

        return args;
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
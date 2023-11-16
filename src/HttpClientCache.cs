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

                if (argsDict["pooledConnectionLifetime"] is TimeSpan timeSpan)
                    socketsHandler.PooledConnectionLifetime = timeSpan;
                else
                    socketsHandler.PooledConnectionLifetime = TimeSpan.FromMinutes(10);

                if (argsDict["cookieContainer"] is true)
                    socketsHandler.CookieContainer = new CookieContainer();

                if (argsDict["maxConnectionsPerServer"] is int maxConnectionsPerServer)
                    socketsHandler.MaxConnectionsPerServer = maxConnectionsPerServer;
                else
                    socketsHandler.MaxConnectionsPerServer = 40;
            }

            var httpClient = new HttpClient(socketsHandler);

            if (argsDict?["timeout"] is TimeSpan timeout)
                httpClient.Timeout = timeout;

            return httpClient;
        });
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
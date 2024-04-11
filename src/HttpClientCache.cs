using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Soenneker.Utils.HttpClientCache.Abstract;
using Soenneker.Utils.Runtime;
using Soenneker.Utils.SingletonDictionary;

namespace Soenneker.Utils.HttpClientCache;

///<inheritdoc cref="IHttpClientCache"/>
public class HttpClientCache : IHttpClientCache
{
    private readonly SingletonDictionary<HttpClient> _httpClients;

    private const string _defaultRequestHeaders = "defaultRequestHeaders";
    private const string _timeout = "timeout";
    private const string _maxConnectionsPerServer = "maxConnectionsPerServer";
    private const string _cookieContainer = "cookieContainer";
    private const string _pooledConnectionLifetime = "pooledConnectionLifetime";

    public HttpClientCache()
    {
        _httpClients = new SingletonDictionary<HttpClient>(args =>
        {
            var socketsHandler = new SocketsHttpHandler();

            var argsDict = (Dictionary<string, object>?) args?.FirstOrDefault();

            SetCookieContainer(socketsHandler, argsDict);
            SetPooledConnectionLifetime(socketsHandler, argsDict);
            SetMaxConnectionsPerServer(socketsHandler, argsDict);

            var httpClient = new HttpClient(socketsHandler)
            {
                Timeout = GetTimeout(argsDict)
            };

            AddDefaultRequestHeaders(httpClient, argsDict);

            return httpClient;
        });
    }

    private static TimeSpan GetTimeout(Dictionary<string, object>? args)
    {
        if (args != null)
        {
            if (args.TryGetValue(_timeout, out object? timeout))
            {
                if (timeout is TimeSpan timespan)
                    return timespan;
            }
        }

        return TimeSpan.FromSeconds(100);
    }

    private static void AddDefaultRequestHeaders(HttpClient httpClient, Dictionary<string, object>? args)
    {
        if (args == null)
            return;

        if (args.TryGetValue(_defaultRequestHeaders, out object? httpRequestHeaders))
        {
            if (httpRequestHeaders is not Dictionary<string, string> headersDict)
                return;

            foreach (KeyValuePair<string, string> keyValuePair in headersDict)
            {
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation(keyValuePair.Key, keyValuePair.Value);
            }
        }
    }

    private static void SetCookieContainer(SocketsHttpHandler socketsHandler, Dictionary<string, object>? args)
    {
        if (args == null)
            return;

        if (args.TryGetValue(_cookieContainer, out object? cookieContainerObj))
        {
            if (cookieContainerObj is bool cookieContainer)
            {
                if (cookieContainer)
                {
                    socketsHandler.CookieContainer = new CookieContainer();
                }
            }
        }
    }

    private static void SetPooledConnectionLifetime(SocketsHttpHandler socketsHandler, Dictionary<string, object>? args)
    {
        if (RuntimeUtil.IsBrowser())
            return;

        if (args != null)
        {
            if (args.TryGetValue(_pooledConnectionLifetime, out object? timeSpanObj))
            {
                if (timeSpanObj is TimeSpan timeSpan)
                    socketsHandler.PooledConnectionLifetime = timeSpan;
            }
        }

        socketsHandler.PooledConnectionLifetime = TimeSpan.FromMinutes(10);
    }

    private static void SetMaxConnectionsPerServer(SocketsHttpHandler socketsHandler, Dictionary<string, object>? args)
    {
        if (RuntimeUtil.IsBrowser())
            return;

        if (args != null)
        {
            if (args.TryGetValue(_maxConnectionsPerServer, out object? value))
            {
                if (value is int maxConnectionsPerServer)
                    socketsHandler.MaxConnectionsPerServer = maxConnectionsPerServer;
            }
        }

        socketsHandler.MaxConnectionsPerServer = 40;
    }

    public ValueTask<HttpClient> Get(string id, TimeSpan? pooledConnectionLifetime = null, bool? cookieContainer = null,
        int? maxConnectionsPerServer = null, TimeSpan? timeout = null, Dictionary<string, string>? defaultRequestHeaders = null)
    {
        if (NoConfigIsSet(pooledConnectionLifetime, cookieContainer, maxConnectionsPerServer, timeout, defaultRequestHeaders))
            return _httpClients.Get(id);

        Dictionary<string, object> args = GetArgsDict(pooledConnectionLifetime, cookieContainer, maxConnectionsPerServer, timeout, defaultRequestHeaders);

        return _httpClients.Get(id, args);
    }

    public HttpClient GetSync(string id, TimeSpan? pooledConnectionLifetime = null, bool? cookieContainer = null,
        int? maxConnectionsPerServer = null, TimeSpan? timeout = null, Dictionary<string, string>? defaultRequestHeaders = null)
    {
        if (NoConfigIsSet(pooledConnectionLifetime, cookieContainer, maxConnectionsPerServer, timeout, defaultRequestHeaders))
            return _httpClients.GetSync(id);

        Dictionary<string, object> args = GetArgsDict(pooledConnectionLifetime, cookieContainer, maxConnectionsPerServer, timeout, defaultRequestHeaders);

        return _httpClients.GetSync(id, args);
    }

    private static bool NoConfigIsSet(TimeSpan? pooledConnectionLifetime = null, bool? cookieContainer = null,
        int? maxConnectionsPerServer = null, TimeSpan? timeout = null, Dictionary<string, string>? defaultRequestHeaders = null)
    {
        return pooledConnectionLifetime == null && cookieContainer == null && maxConnectionsPerServer == null && timeout == null && defaultRequestHeaders == null;
    }

    private static Dictionary<string, object> GetArgsDict(TimeSpan? pooledConnectionLifetime = null, bool? cookieContainer = null,
        int? maxConnectionsPerServer = null, TimeSpan? timeout = null, Dictionary<string, string>? defaultRequestHeaders = null)
    {
        var args = new Dictionary<string, object>();

        if (pooledConnectionLifetime != null)
            args.Add(_pooledConnectionLifetime, pooledConnectionLifetime);

        if (cookieContainer != null)
            args.Add(_cookieContainer, cookieContainer);

        if (maxConnectionsPerServer != null)
            args.Add(_maxConnectionsPerServer, maxConnectionsPerServer);

        if (timeout != null)
            args.Add(_timeout, timeout);

        if (defaultRequestHeaders != null)
            args.Add(_defaultRequestHeaders, defaultRequestHeaders);

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
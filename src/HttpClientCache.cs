using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Soenneker.Utils.HttpClientCache.Abstract;
using Soenneker.Utils.SingletonDictionary;

namespace Soenneker.Utils.HttpClientCache;

///<inheritdoc cref="IHttpClientCache"/>
public class HttpClientCache : IHttpClientCache
{
    private readonly SingletonDictionary<HttpClient> _httpClients;

    public HttpClientCache()
    {
        _httpClients = new SingletonDictionary<HttpClient>((args) =>
        {
            var socketsHandler = new SocketsHttpHandler
            {
                // TOOD: make configurable
                PooledConnectionLifetime = TimeSpan.FromMinutes(10),
            };

            if (args?.FirstOrDefault() is Dictionary<string, object> dict)
            {
                if (dict["cookieContainer"] is true)
                {
                    socketsHandler.CookieContainer = new CookieContainer();
                }

                socketsHandler.MaxConnectionsPerServer = (int) dict["maxConnectionsPerServer"];
            }

            var httpClient = new HttpClient(socketsHandler);

            return httpClient;
        });
    }

    public ValueTask<HttpClient> GetClient(string id, bool cookieContainer = false, int maxConnectionsPerServer = 40)
    {
        var args = new Dictionary<string, object>
        {
            {nameof(cookieContainer), cookieContainer},
            {nameof(maxConnectionsPerServer), maxConnectionsPerServer}
        };

        return _httpClients.Get(id, args);
    }
}
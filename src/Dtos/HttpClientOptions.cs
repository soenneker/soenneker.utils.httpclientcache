using System;
using System.Collections.Generic;
using System.Net.Http;

namespace Soenneker.Utils.HttpClientCache.Dtos;

public record HttpClientOptions
{
    public TimeSpan? PooledConnectionLifetime { get; set; }

    public bool? UseCookieContainer { get; set; }

    public int? MaxConnectionsPerServer { get; set; }

    public TimeSpan? Timeout { get; set; }

    public Dictionary<string, string>? DefaultRequestHeaders { get; set; }

    public Action<HttpClient>? ModifyClient { get; set; }
}
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Soenneker.Utils.HttpClientCache.Dtos;

/// <summary>
/// Represents options for configuring an <see cref="HttpClient"/> instance.
/// </summary>
public record HttpClientOptions
{
    /// <summary>
    /// Gets or sets the maximum lifetime of a connection in the connection pool before it is discarded.
    /// A value of <see langword="null"/> indicates that the connection will not have a limited lifetime.
    /// </summary>
    public TimeSpan? PooledConnectionLifetime { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the <see cref="HttpClient"/> should use a cookie container to store and manage cookies.
    /// A value of <see langword="null"/> indicates that the default behavior of the client will be used.
    /// </summary>
    public bool? UseCookieContainer { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of concurrent connections allowed per server.
    /// A value of <see langword="null"/> indicates that the default value will be used.
    /// </summary>
    public int? MaxConnectionsPerServer { get; set; }

    /// <summary>
    /// Gets or sets the time to wait before the request times out.
    /// A value of <see langword="null"/> indicates that the default timeout will be used.
    /// </summary>
    public TimeSpan? Timeout { get; set; }

    /// <summary>
    /// Gets or sets a collection of default headers to be included with each request.
    /// A value of <see langword="null"/> indicates that no default headers will be added.
    /// </summary>
    public Dictionary<string, string>? DefaultRequestHeaders { get; set; }

    /// <summary>
    /// Gets or sets a function to modify the <see cref="HttpClient"/> after it has been created.
    /// This function is only executed during the first retrieval of the client.
    /// </summary>
    public Func<HttpClient, ValueTask>? ModifyClient { get; set; }

    /// <summary>
    /// Gets or sets the base address of the <see cref="HttpClient"/> as a string.
    /// A value of <see langword="null"/> indicates that no base address will be set.
    /// </summary>
    public string? BaseAddress { get; set; }
}
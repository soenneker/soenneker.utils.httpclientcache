using System;
using System.Diagnostics.Contracts;
using System.Net.Http;
using System.Threading.Tasks;

namespace Soenneker.Utils.HttpClientCache.Abstract;

/// <summary>
/// A utility library for singleton thread-safe HttpClients
/// </summary>
public interface IHttpClientCache : IAsyncDisposable, IDisposable
{
    /// <summary>
    /// Retrieves an <see cref="HttpClient"/> from the cache. New parameters will not be applied to existing clients
    /// </summary>
    /// <param name="id">The id that this is looked up by in the cache</param>
    /// <param name="pooledConnectionLifetime">If null, this is 10minutes</param>
    /// <param name="cookieContainer">If this is null, cookie support is not enabled</param>
    /// <param name="maxConnectionsPerServer">If this is null, this is set to 40</param>
    /// <param name="timeout">If this is null, this is set to the default 100s</param>
    /// <returns></returns>
    [Pure]
    ValueTask<HttpClient> Get(string id, TimeSpan? pooledConnectionLifetime = null, bool? cookieContainer = null, int? maxConnectionsPerServer = null, TimeSpan? timeout = null);

    /// <inheritdoc cref="Get(string, TimeSpan?, bool?, int?, TimeSpan?)"/>"/>
    /// <remarks><see cref="Get"/> async method is recommended</remarks>
    [Pure]
    HttpClient GetSync(string id, TimeSpan? pooledConnectionLifetime = null, bool? cookieContainer = null,
        int? maxConnectionsPerServer = null, TimeSpan? timeout = null);

    /// <summary>
    /// Should be used if the component using <see cref="IHttpClientCache"/> is disposed (unless the entire app is being disposed). Includes disposal of the <see cref="HttpClient"/>.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    ValueTask Remove(string id);

    /// <inheritdoc cref="Remove(string)"/>"/>
    void RemoveSync(string id);
}
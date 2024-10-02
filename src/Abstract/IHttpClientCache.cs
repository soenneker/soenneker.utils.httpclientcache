using Soenneker.Utils.HttpClientCache.Dtos;
using System;
using System.Diagnostics.Contracts;
using System.Net.Http;
using System.Threading;
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
    /// <param name="httpClientOptions">If null, this is 10minutes</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [Pure]
    ValueTask<HttpClient> Get(string id, HttpClientOptions? httpClientOptions = null, CancellationToken cancellationToken = default);

    /// <inheritdoc cref="Get(string,HttpClientOptions?, CancellationToken)"/>"/>
    /// <remarks><see cref="Get"/> async method is recommended</remarks>
    [Pure]
    HttpClient GetSync(string id, HttpClientOptions? httpClientOptions = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Should be used if the component using <see cref="IHttpClientCache"/> is disposed (unless the entire app is being disposed). Includes disposal of the <see cref="HttpClient"/>.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    ValueTask Remove(string id);

    /// <inheritdoc cref="Remove(string)"/>"/>
    void RemoveSync(string id);
}
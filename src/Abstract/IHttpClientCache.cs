using Soenneker.Utils.HttpClientCache.Dtos;
using System;
using System.Diagnostics.Contracts;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Utils.HttpClientCache.Abstract;

/// <summary>
/// A utility library for singleton thread-safe <see cref="HttpClient"/>s
/// </summary>
public interface IHttpClientCache : IAsyncDisposable, IDisposable
{
    /// <summary>
    /// Retrieves an <see cref="HttpClient"/> from the cache. New parameters will not be applied to existing clients.
    /// </summary>
    /// <param name="id">The id that this is looked up by in the cache</param>
    /// <param name="httpClientOptions">Options for configuring the client. Null uses defaults</param>
    /// <param name="cancellationToken"></param>
    [Pure]
    ValueTask<HttpClient> Get(string id, HttpClientOptions? httpClientOptions = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves an <see cref="HttpClient"/> using a synchronous factory to provide <see cref="HttpClientOptions"/>.
    /// Factory is only invoked if the client doesn't exist in the cache.
    /// </summary>
    /// <param name="id">The cache key</param>
    /// <param name="optionsFactory">A synchronous delegate that returns the options</param>
    /// <param name="cancellationToken"></param>
    [Pure]
    ValueTask<HttpClient> Get(string id, Func<HttpClientOptions?> optionsFactory, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves an <see cref="HttpClient"/> using an asynchronous factory to provide <see cref="HttpClientOptions"/>.
    /// Factory is only invoked if the client doesn't exist in the cache.
    /// </summary>
    /// <param name="id">The cache key</param>
    /// <param name="optionsFactory">An asynchronous delegate that returns the options</param>
    /// <param name="cancellationToken"></param>
    [Pure]
    ValueTask<HttpClient> Get(string id, Func<ValueTask<HttpClientOptions?>> optionsFactory, CancellationToken cancellationToken = default);

    /// <inheritdoc cref="Get(string, HttpClientOptions?, CancellationToken)"/>
    /// <remarks><see cref="Get"/> async method is recommended</remarks>
    [Pure]
    HttpClient GetSync(string id, HttpClientOptions? httpClientOptions = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Same as <see cref="GetSync(string, HttpClientOptions?, CancellationToken)"/>, but uses a synchronous factory for <see cref="HttpClientOptions"/>.
    /// </summary>
    /// <param name="id">The cache key</param>
    /// <param name="optionsFactory">Synchronous factory for options</param>
    /// <param name="cancellationToken"></param>
    [Pure]
    HttpClient GetSync(string id, Func<HttpClientOptions?> optionsFactory, CancellationToken cancellationToken = default);

    /// <summary>
    /// Should be used if the component using <see cref="IHttpClientCache"/> is disposed (unless the entire app is being disposed). Includes disposal of the <see cref="HttpClient"/>.
    /// </summary>
    /// <param name="id">The cache key</param>
    ValueTask Remove(string id);

    /// <inheritdoc cref="Remove(string)"/>
    void RemoveSync(string id);
}

using System;
using System.Diagnostics.Contracts;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Soenneker.Dtos.HttpClientOptions;

namespace Soenneker.Utils.HttpClientCache.Abstract;

/// <summary>
/// A utility library for singleton thread-safe <see cref="HttpClient"/>s
/// </summary>
public interface IHttpClientCache : IAsyncDisposable, IDisposable
{
    /// <summary>
    /// Retrieves an <see cref="HttpClient"/> with default options.
    /// </summary>
    /// <param name="id">The cache key</param>
    /// <param name="cancellationToken"></param>
    [Pure]
    ValueTask<HttpClient> Get(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves an <see cref="HttpClient"/> using an asynchronous factory to provide <see cref="HttpClientOptions"/>.
    /// Factory is only invoked if the client doesn't exist in the cache.
    /// </summary>
    /// <param name="id">The cache key</param>
    /// <param name="optionsFactory">An asynchronous delegate that returns the options</param>
    /// <param name="cancellationToken"></param>
    [Pure]
    ValueTask<HttpClient> Get(string id, Func<CancellationToken, ValueTask<HttpClientOptions?>> optionsFactory, CancellationToken cancellationToken = default);

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

    /// <summary>
    /// Retrieves an <see cref="HttpClient"/> with default options synchronously.
    /// </summary>
    /// <param name="id">The cache key</param>
    /// <param name="cancellationToken"></param>
    [Pure]
    HttpClient GetSync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves an <see cref="HttpClient"/> using an asynchronous factory to provide <see cref="HttpClientOptions"/> synchronously.
    /// Factory is only invoked if the client doesn't exist in the cache.
    /// </summary>
    /// <param name="id">The cache key</param>
    /// <param name="optionsFactory">An asynchronous delegate that returns the options</param>
    /// <param name="cancellationToken"></param>
    [Pure]
    HttpClient GetSync(string id, Func<CancellationToken, ValueTask<HttpClientOptions?>> optionsFactory, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves an <see cref="HttpClient"/> using a synchronous factory to provide <see cref="HttpClientOptions"/> synchronously.
    /// Factory is only invoked if the client doesn't exist in the cache.
    /// </summary>
    /// <param name="id">The cache key</param>
    /// <param name="optionsFactory">A synchronous delegate that returns the options</param>
    /// <param name="cancellationToken"></param>
    [Pure]
    HttpClient GetSync(string id, Func<HttpClientOptions?> optionsFactory, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a configured and ready-to-use synchronous instance of <see cref="HttpClient"/> associated with the
    /// specified identifier.
    /// </summary>
    /// <remarks>This method blocks until the client is fully configured and available. If the options factory
    /// performs asynchronous operations, they will be completed before the client is returned. The returned <see
    /// cref="HttpClient"/> is intended for reuse and should not be disposed by the caller.</remarks>
    /// <param name="id">The unique identifier for the <see cref="HttpClient"/> instance to retrieve. Cannot be null or empty.</param>
    /// <param name="optionsFactory">A factory delegate that asynchronously provides <see cref="HttpClientOptions"/> for configuring the client. The
    /// returned options may be null to use default settings.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation before completion. Optional.</param>
    /// <returns>An <see cref="HttpClient"/> instance configured according to the provided options. The same instance may be
    /// returned for repeated calls with the same identifier.</returns>
    [Pure]
    HttpClient GetSync(string id, Func<ValueTask<HttpClientOptions?>> optionsFactory, CancellationToken cancellationToken = default);

    [Pure]
    ValueTask<HttpClient> Get<TState>(
        string id,
        TState state,
        Func<TState, HttpClientOptions?> optionsFactory,
        CancellationToken cancellationToken = default)
        where TState : notnull;

    [Pure]
    ValueTask<HttpClient> Get<TState>(
        string id,
        TState state,
        Func<TState, CancellationToken, ValueTask<HttpClientOptions?>> optionsFactory,
        CancellationToken cancellationToken = default)
        where TState : notnull;

    /// <summary>
    /// Should be used if the component using <see cref="IHttpClientCache"/> is disposed (unless the entire app is being disposed). Includes disposal of the <see cref="HttpClient"/>.
    /// </summary>
    /// <param name="id">The cache key</param>
    /// <param name="cancellationToken"></param>
    ValueTask Remove(string id, CancellationToken cancellationToken = default);

    /// <inheritdoc cref="Remove(string,CancellationToken)"/>
    void RemoveSync(string id, CancellationToken cancellationToken = default);
}
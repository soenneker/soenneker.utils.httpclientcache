using Soenneker.Dtos.HttpClientOptions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Utils.HttpClientCache;

internal static class StateInvoker<TState> where TState : notnull
{
    internal static readonly Func<object, Delegate, CancellationToken, ValueTask<HttpClientOptions?>> Sync = static (s, d, _) =>
        new(((Func<TState, HttpClientOptions?>)d)((TState)s));

    internal static readonly Func<object, Delegate, CancellationToken, ValueTask<HttpClientOptions?>> Async = static (s, d, _) =>
        ((Func<TState, ValueTask<HttpClientOptions?>>)d)((TState)s);

    internal static readonly Func<object, Delegate, CancellationToken, ValueTask<HttpClientOptions?>> TokenAsync = static (s, d, ct) =>
        ((Func<TState, CancellationToken, ValueTask<HttpClientOptions?>>)d)((TState)s, ct);
}

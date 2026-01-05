using Soenneker.Dtos.HttpClientOptions;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Utils.HttpClientCache;

internal readonly struct OptionsFactory
{
    // kind:
    // 0 = null
    // 1 = token async
    // 2 = sync
    // 3 = async
    // 4 = state sync
    // 5 = state token async
    // 6 = state async
    private readonly byte _kind;

    private readonly Func<CancellationToken, ValueTask<HttpClientOptions?>>? _tokenAsync;
    private readonly Func<HttpClientOptions?>? _sync;
    private readonly Func<ValueTask<HttpClientOptions?>>? _async;

    private readonly object? _state;
    private readonly Delegate? _stateFactory;
    private readonly Func<object, Delegate, CancellationToken, ValueTask<HttpClientOptions?>>? _stateInvoker;

    private OptionsFactory(byte kind, Func<CancellationToken, ValueTask<HttpClientOptions?>>? tokenAsync, Func<HttpClientOptions?>? sync,
        Func<ValueTask<HttpClientOptions?>>? async, object? state, Delegate? stateFactory,
        Func<object, Delegate, CancellationToken, ValueTask<HttpClientOptions?>>? stateInvoker)
    {
        _kind = kind;
        _tokenAsync = tokenAsync;
        _sync = sync;
        _async = async;
        _state = state;
        _stateFactory = stateFactory;
        _stateInvoker = stateInvoker;
    }

    public static OptionsFactory Null => default;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OptionsFactory From(Func<CancellationToken, ValueTask<HttpClientOptions?>> factory) =>
        new(kind: 1, tokenAsync: factory, sync: null, async: null, state: null, stateFactory: null, stateInvoker: null);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OptionsFactory From(Func<HttpClientOptions?> factory) =>
        new(kind: 2, tokenAsync: null, sync: factory, async: null, state: null, stateFactory: null, stateInvoker: null);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OptionsFactory From(Func<ValueTask<HttpClientOptions?>> factory) =>
        new(kind: 3, tokenAsync: null, sync: null, async: factory, state: null, stateFactory: null, stateInvoker: null);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OptionsFactory From<TState>(TState state, Func<TState, HttpClientOptions?> factory) where TState : notnull =>
        new(kind: 4, tokenAsync: null, sync: null, async: null, state: state, stateFactory: factory, stateInvoker: StateInvoker<TState>.Sync);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OptionsFactory From<TState>(TState state, Func<TState, CancellationToken, ValueTask<HttpClientOptions?>> factory) where TState : notnull =>
        new(kind: 5, tokenAsync: null, sync: null, async: null, state: state, stateFactory: factory, stateInvoker: StateInvoker<TState>.TokenAsync);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OptionsFactory From<TState>(TState state, Func<TState, ValueTask<HttpClientOptions?>> factory) where TState : notnull =>
        new(kind: 6, tokenAsync: null, sync: null, async: null, state: state, stateFactory: factory, stateInvoker: StateInvoker<TState>.Async);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<HttpClientOptions?> Invoke(CancellationToken cancellationToken)
    {
        return _kind switch
        {
            0 => default,
            1 => _tokenAsync!(cancellationToken),
            2 => new ValueTask<HttpClientOptions?>(_sync!()),
            3 => _async!(),
            4 or 5 or 6 => _stateInvoker!(_state!, _stateFactory!, cancellationToken),
            _ => default
        };
    }

    private static class StateInvoker<TState> where TState : notnull
    {
        internal static readonly Func<object, Delegate, CancellationToken, ValueTask<HttpClientOptions?>> Sync = static (s, d, _) =>
            new(((Func<TState, HttpClientOptions?>)d)((TState)s));

        internal static readonly Func<object, Delegate, CancellationToken, ValueTask<HttpClientOptions?>> Async = static (s, d, _) =>
            ((Func<TState, ValueTask<HttpClientOptions?>>)d)((TState)s);

        internal static readonly Func<object, Delegate, CancellationToken, ValueTask<HttpClientOptions?>> TokenAsync = static (s, d, ct) =>
            ((Func<TState, CancellationToken, ValueTask<HttpClientOptions?>>)d)((TState)s, ct);
    }
}
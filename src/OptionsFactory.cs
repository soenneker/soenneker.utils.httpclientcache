using Soenneker.Dtos.HttpClientOptions;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Utils.HttpClientCache;

internal readonly struct OptionsFactory
{
    private readonly byte _kind; // 0=null, 1=token async, 2=sync, 3=async
    private readonly Func<CancellationToken, ValueTask<HttpClientOptions?>>? _tokenAsync;
    private readonly Func<HttpClientOptions?>? _sync;
    private readonly Func<ValueTask<HttpClientOptions?>>? _async;

    private OptionsFactory(byte kind, Func<CancellationToken, ValueTask<HttpClientOptions?>>? tokenAsync, Func<HttpClientOptions?>? sync,
        Func<ValueTask<HttpClientOptions?>>? async)
    {
        _kind = kind;
        _tokenAsync = tokenAsync;
        _sync = sync;
        _async = async;
    }

    public static OptionsFactory Null => default;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OptionsFactory From(Func<CancellationToken, ValueTask<HttpClientOptions?>> factory) =>
        new(kind: 1, tokenAsync: factory, sync: null, async: null);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OptionsFactory From(Func<HttpClientOptions?> factory) =>
        new(kind: 2, tokenAsync: null, sync: factory, async: null);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OptionsFactory From(Func<ValueTask<HttpClientOptions?>> factory) =>
        new(kind: 3, tokenAsync: null, sync: null, async: factory);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<HttpClientOptions?> Invoke(CancellationToken cancellationToken)
    {
        return _kind switch
        {
            0 => default,
            1 => _tokenAsync!(cancellationToken),
            2 => new ValueTask<HttpClientOptions?>(_sync!()),
            3 => _async!(),
            _ => default
        };
    }
}
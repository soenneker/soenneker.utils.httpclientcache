using System.Net;
using System.Net.Http;
using System.Net.Security;

namespace Soenneker.Utils.HttpClientCache;

internal readonly record struct HandlerKey(
    long PooledConnectionLifetimeTicks,
    int MaxConnectionsPerServer,
    bool UseCookies,
    long ConnectTimeoutTicks,
    long? ResponseDrainTimeoutTicks,
    bool? AllowAutoRedirect,
    DecompressionMethods? AutomaticDecompression,
    long? KeepAlivePingDelayTicks,
    long? KeepAlivePingTimeoutTicks,
    HttpKeepAlivePingPolicy? KeepAlivePingPolicy,
    bool? UseProxy,
    IWebProxy? Proxy,
    int? MaxResponseDrainSize,
    int? MaxResponseHeadersLength,
    SslClientAuthenticationOptions? SslOptions);
namespace Soenneker.Utils.HttpClientCache;

internal readonly record struct HandlerKey(
    double LifetimeSeconds,
    int MaxConnections,
    bool UseCookies,
    double ConnectTimeoutSeconds,
    double? ResponseDrainTimeoutSeconds,
    bool? AllowAutoRedirect,
    int? AutomaticDecompression,
    double? KeepAlivePingDelaySeconds,
    double? KeepAlivePingTimeoutSeconds,
    int? KeepAlivePingPolicy,
    bool? UseProxy,
    int? ProxyHashCode,
    int? MaxResponseDrainSize,
    int? MaxResponseHeadersLength,
    int? SslOptionsHashCode);
namespace Soenneker.Utils.HttpClientCache;

internal readonly record struct HandlerKey(double LifetimeSeconds, int MaxConnections, bool UseCookies);
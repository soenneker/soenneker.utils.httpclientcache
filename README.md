[![](https://img.shields.io/nuget/v/Soenneker.Utils.HttpClientCache.svg?style=for-the-badge)](https://www.nuget.org/packages/Soenneker.Utils.HttpClientCache/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.utils.httpclientcache/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.utils.httpclientcache/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/Soenneker.Utils.HttpClientCache.svg?style=for-the-badge)](https://www.nuget.org/packages/Soenneker.Utils.HttpClientCache/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.utils.httpclientcache/codeql.yml?label=CodeQL&style=for-the-badge)](https://github.com/soenneker/soenneker.utils.httpclientcache/actions/workflows/codeql.yml)

# Soenneker.Utils.HttpClientCache

Provides lazily created, reusable `HttpClient` instances keyed by application-defined identifiers.

## Installation

```bash
dotnet add package Soenneker.Utils.HttpClientCache
```

## Registration

```csharp
using Soenneker.Utils.HttpClientCache.Registrar;

services.AddHttpClientCacheAsSingleton();
```

Use `AddHttpClientCacheAsScoped()` only when every dependency-injection scope should own and dispose an independent cache.

## Create or retrieve a client

```csharp
using System.Net;
using Soenneker.Dtos.HttpClientOptions;
using Soenneker.Utils.HttpClientCache.Abstract;

HttpClient client = await cache.Get(
    "catalog-api",
    static () => new HttpClientOptions
    {
        BaseAddress = new Uri("https://catalog.example.com/"),
        Timeout = TimeSpan.FromSeconds(30),
        ConnectTimeout = TimeSpan.FromSeconds(5),
        AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
        DefaultRequestHeaders = new Dictionary<string, string>
        {
            ["Accept"] = "application/json"
        }
    },
    cancellationToken);
```

The options factory runs only when that key is first initialized. Later calls with the same key return the same `HttpClient` and ignore new factories/options. Use a stable, unique key for each logical client configuration.

Do not dispose a returned client. Remove and dispose one cache entry with `Remove`/`RemoveSync`, or dispose the cache to release all owned clients and dedicated handlers.

## Handler behavior

Handlers with the same transport options can be shared across cache keys. Cookies are disabled on shared handlers. Setting `UseCookieContainer = true` creates a dedicated, cache-owned handler and cookie container for that client so cookies are not shared between keys.

`ModifyPrimaryHandler`, `Proxy`, `SslOptions`, or delegating handlers also require dedicated transports. Delegating handler factories run once during client creation; each must return a new handler with `InnerHandler` unset. The first factory in the list becomes the outermost handler.

`ModifyClient` runs once after base address, timeout, and default headers are applied. It can perform asynchronous initialization.

## Security and lifecycle notes

- Default request headers are added without semantic validation. Use only trusted header names and values.
- Automatic redirects can forward non-`Authorization` custom headers to another host. Disable redirects for clients carrying secrets in custom headers unless every redirect target is trusted.
- Custom certificate validation in `SslOptions` or `ModifyPrimaryHandler` changes the TLS trust boundary; avoid permissive callbacks.
- The cache does not retry, enforce successful status codes, or dispose response messages. Callers own each response and response stream.
- Browser targets do not support custom primary/delegating handler configuration.

Synchronous `GetSync` overloads are available for callers that cannot use asynchronous initialization. Prefer asynchronous `Get` when an options factory performs I/O.

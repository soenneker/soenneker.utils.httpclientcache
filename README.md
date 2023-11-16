[![](https://img.shields.io/nuget/v/Soenneker.Utils.HttpClientCache.svg?style=for-the-badge)](https://www.nuget.org/packages/Soenneker.Utils.HttpClientCache/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.utils.httpclientcache/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.utils.httpclientcache/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/Soenneker.Utils.HttpClientCache.svg?style=for-the-badge)](https://www.nuget.org/packages/Soenneker.Utils.HttpClientCache/)

# ![](https://user-images.githubusercontent.com/4441470/224455560-91ed3ee7-f510-4041-a8d2-3fc093025112.png) Soenneker.Utils.HttpClientCache
### Providing thread-safe singleton HttpClients

### Why

'Long-lived' `HttpClient` static/singleton instances are the recommended use pattern in .NET. Avoid the unnecessary overhead of `IHttpClientFactory`, and _definitely_ avoid creating a new `HttpClient` instance per request.

See [Guidelines for using HttpClient](https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/http/httpclient-guidelines)

`HttpClientCache` provides a thread-safe singleton `HttpClient` instance per key. The `HttpClient` is created on first access and disposed on application shutdown (or manual removal/disposal).

## Installation

```
Install-Package Soenneker.Utils.HttpClientCache
```


## Usage

1. Register the interop within DI (`Program.cs`).

```csharp
public static async Task Main(string[] args)
{
    ...
    builder.Services.AddHttpClientCache();
}
```

2. Inject `IHttpClientCache` via constructor, and retrieve a fresh `HttpClient`.

Example:

```csharp
public class TestClass
{
    IHttpClientCache _httpClientCache;

    public TestClass(IHttpClientCache httpClientCache)
    {
        _httpClientCache = httpClientCache;
    }

    public async ValueTask<string> GetGoogleSource()
    {
        HttpClient httpClient = await _httpClientCache.Get(nameof(TestClass));

        var response = await httpClient.GetAsync("https://www.google.com");
        response.EnsureSuccessStatusCode();

        var responseString = await response.Content.ReadAsStringAsync();
        return responseString;
    }
}
```
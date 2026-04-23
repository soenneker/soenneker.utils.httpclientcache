using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Soenneker.Dtos.HttpClientOptions;
using Soenneker.Tests.Unit;

namespace Soenneker.Utils.HttpClientCache.Tests;

public class HttpClientCacheTests : UnitTest
{
    [Test]
    public async Task Get_should_not_be_null_with_null_parameters(CancellationToken cancellationToken)
    {
        var httpClientCache = new HttpClientCache();

        HttpClient httpClient = await httpClientCache.Get("test", cancellationToken: cancellationToken);

        httpClient.Should().NotBeNull();
    }

    [Test]
    public async Task Get_should_not_be_null_with_parameters(CancellationToken cancellationToken)
    {
        var httpClientCache = new HttpClientCache();


        HttpClient httpClient = await httpClientCache.Get("test", static () => new HttpClientOptions
        {
            Timeout = TimeSpan.FromMinutes(10)
        }, cancellationToken);
        httpClient.Should().NotBeNull();
    }

    [Test]
    public async Task Get_with_modifications_should_persist_in_cache(CancellationToken cancellationToken)
    {
        var httpClientCache = new HttpClientCache();

        HttpClient httpClient1 = await httpClientCache.Get("test", static () => new HttpClientOptions
        {
            Timeout = TimeSpan.FromMinutes(10)
        }, cancellationToken);
        httpClient1.Timeout = TimeSpan.FromMinutes(1);

        HttpClient httpClient2 = await httpClientCache.Get("test", static () => new HttpClientOptions
        {
            Timeout = TimeSpan.FromMinutes(10)
        }, cancellationToken);
        httpClient2.Timeout.TotalMinutes.Should().Be(1);
    }
}
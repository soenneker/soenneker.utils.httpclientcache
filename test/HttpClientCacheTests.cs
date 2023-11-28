using System;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Soenneker.Tests.Unit;
using Xunit;

namespace Soenneker.Utils.HttpClientCache.Tests;

public class HttpClientCacheTests : UnitTest
{
    [Fact]
    public async Task Get_should_not_be_null_with_null_parameters()
    {
        var httpClientCache = new HttpClientCache();

        HttpClient httpClient = await httpClientCache.Get("test");

        httpClient.Should().NotBeNull();
    }

    [Fact]
    public async Task Get_should_not_be_null_with_parameters()
    {
        var httpClientCache = new HttpClientCache();

        HttpClient httpClient = await httpClientCache.Get("test", TimeSpan.FromMinutes(10), true, 40);
        httpClient.Should().NotBeNull();
    }

    [Fact]
    public async Task Get_should_not_be_null_with_partial_parameters()
    {
        var httpClientCache = new HttpClientCache();

        HttpClient httpClient = await httpClientCache.Get("test", TimeSpan.FromMinutes(10));
        httpClient.Should().NotBeNull();
    }
}
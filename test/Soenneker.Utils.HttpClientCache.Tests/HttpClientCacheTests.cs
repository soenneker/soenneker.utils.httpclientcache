﻿using System;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Soenneker.Tests.Unit;
using Soenneker.Utils.HttpClientCache.Dtos;
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

        var clientOptions = new HttpClientOptions
        {
            Timeout = TimeSpan.FromMinutes(10)
        };

        HttpClient httpClient = await httpClientCache.Get("test", clientOptions);
        httpClient.Should().NotBeNull();
    }

    [Fact]
    public async Task Get_with_modifications_should_persist_in_cache()
    {
        var httpClientCache = new HttpClientCache();

        var clientOptions = new HttpClientOptions
        {
            Timeout = TimeSpan.FromMinutes(10)
        };

        HttpClient httpClient1 = await httpClientCache.Get("test", clientOptions);
        httpClient1.Timeout = TimeSpan.FromMinutes(1);

        HttpClient httpClient2 = await httpClientCache.Get("test", clientOptions);
        httpClient2.Timeout.TotalMinutes.Should().Be(1);
    }
}
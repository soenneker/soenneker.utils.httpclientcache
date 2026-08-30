using System;
using System.Net.Http;
using System.Net;
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

    [Test]
    public async Task Get_should_compose_delegating_handler_pipeline(CancellationToken cancellationToken)
    {
        var httpClientCache = new HttpClientCache();
        var handler = new StubHandler();
        bool primaryHandlerModified = false;

        HttpClient client = await httpClientCache.Get("pipeline", () => new HttpClientOptions
        {
            DelegatingHandlerFactories = [() => handler],
            PooledConnectionLifetime = TimeSpan.FromMinutes(3),
            ModifyPrimaryHandler = primaryHandler =>
            {
                primaryHandlerModified = true;
                primaryHandler.UseProxy = false;
            }
        }, cancellationToken);

        using HttpResponseMessage response = await client.GetAsync("https://example.test", cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        handler.Called.Should().BeTrue();
        handler.InnerHandler.Should().BeOfType<SocketsHttpHandler>()
               .Which.PooledConnectionLifetime.Should().Be(TimeSpan.FromMinutes(3));
        primaryHandlerModified.Should().BeTrue();
        ((SocketsHttpHandler)handler.InnerHandler).UseProxy.Should().BeFalse();
        ((SocketsHttpHandler)handler.InnerHandler).UseCookies.Should().BeFalse();

        await httpClientCache.Remove("pipeline");
        handler.Disposed.Should().BeTrue();
    }

    private sealed class StubHandler : DelegatingHandler
    {
        public bool Called { get; private set; }
        public bool Disposed { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Called = true;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Accepted));
        }

        protected override void Dispose(bool disposing)
        {
            Disposed = true;
            base.Dispose(disposing);
        }
    }
}

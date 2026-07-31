using System.Net;
using System.Text;
using FluentAssertions;
using Newtonsoft.Json;
using OpenPayments.Sdk.Exceptions;
using OpenPayments.Sdk.Http;

namespace OpenPayments.Sdk.Tests.Http;

public class OpenPaymentsClientBase_Tests
{
    private sealed record CapturedRequest(
        HttpMethod Method,
        Uri? Uri,
        string? AuthorizationHeader,
        string? AcceptHeader,
        string? Body,
        string? ContentType
    );

    /// <summary>
    /// Captures the request as scalars (the client disposes the request after sending)
    /// and returns a canned response.
    /// </summary>
    private sealed class CapturingHandler(
        HttpStatusCode status = HttpStatusCode.OK,
        string responseBody = "{}"
    ) : HttpMessageHandler
    {
        public CapturedRequest? Captured { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            Captured = new CapturedRequest(
                request.Method,
                request.RequestUri,
                request.Headers.Authorization?.ToString(),
                request.Headers.Accept.Count == 0 ? null : request.Headers.Accept.ToString(),
                request.Content is null
                    ? null
                    : await request.Content.ReadAsStringAsync(cancellationToken),
                request.Content?.Headers.ContentType?.ToString()
            );

            return new HttpResponseMessage(status)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, "application/json"),
            };
        }
    }

    private sealed class TestClient(HttpClient http, JsonSerializerSettings settings)
        : OpenPaymentsClientBase(http, settings)
    {
        public Task<T> GetAsync<T>(Uri url, string? token) =>
            SendAsync<T>(HttpMethod.Get, url, null, token, CancellationToken.None);

        public Task<T> PostAsync<T>(Uri url, object? body, string? token) =>
            SendAsync<T>(HttpMethod.Post, url, body, token, CancellationToken.None);

        public Task DeleteAsync(Uri url, string? token) =>
            SendAsync(HttpMethod.Delete, url, null, token, CancellationToken.None);
    }

    private sealed class Payload
    {
        public string? Kept { get; set; }
        public string? Dropped { get; set; }
    }

    private static readonly Uri Url = new("https://api.example.com/things");

    private static TestClient Client(
        CapturingHandler handler,
        JsonSerializerSettings? settings = null
    ) => new(new HttpClient(handler), settings ?? new JsonSerializerSettings());

    [Fact]
    public async Task SendAsync_WithAccessToken_SetsGnapAuthorizationHeader()
    {
        var handler = new CapturingHandler();

        await Client(handler).GetAsync<Payload>(Url, "token-123");

        handler.Captured!.AuthorizationHeader.Should().Be("GNAP token-123");
    }

    [Fact]
    public async Task SendAsync_WithoutAccessToken_SendsNoAuthorizationHeader()
    {
        var handler = new CapturingHandler();

        await Client(handler).GetAsync<Payload>(Url, null);

        handler.Captured!.AuthorizationHeader.Should().BeNull();
    }

    [Fact]
    public async Task SendAsync_Typed_AddsJsonAcceptHeader()
    {
        var handler = new CapturingHandler();

        await Client(handler).GetAsync<Payload>(Url, null);

        handler.Captured!.AcceptHeader.Should().Be("application/json");
    }

    [Fact]
    public async Task SendAsync_Void_AddsNoAcceptHeaderAndNoContent()
    {
        var handler = new CapturingHandler(HttpStatusCode.NoContent, "");

        await Client(handler).DeleteAsync(Url, "token-123");

        handler.Captured!.AcceptHeader.Should().BeNull();
        handler.Captured!.Body.Should().BeNull();
    }

    [Fact]
    public async Task SendAsync_WithBody_SerializesWithInjectedSettings()
    {
        var handler = new CapturingHandler();
        var settings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
        };

        await Client(handler, settings)
            .PostAsync<Payload>(Url, new Payload { Kept = "value", Dropped = null }, null);

        handler.Captured!.Body.Should().Be("""{"Kept":"value"}""");
        handler.Captured!.ContentType.Should().Be("application/json");
    }

    [Fact]
    public async Task SendAsync_PostWithNullBody_SendsEmptyJsonBodyWithCharset()
    {
        var handler = new CapturingHandler();

        await Client(handler).PostAsync<Payload>(Url, null, "token-123");

        handler.Captured!.Body.Should().BeEmpty();
        handler.Captured!.ContentType.Should().Be("application/json; charset=utf-8");
    }

    [Fact]
    public async Task SendAsync_OnErrorResponse_ThrowsOpenPaymentsApiException()
    {
        var handler = new CapturingHandler(
            HttpStatusCode.Unauthorized,
            """{"error":{"code":"unauthorized","description":"Bad token"}}"""
        );

        var act = () => Client(handler).GetAsync<Payload>(Url, "token-123");

        var exception = await act.Should().ThrowAsync<OpenPaymentsApiException>();
        exception.Which.StatusCode.Should().Be(401);
        exception.Which.ErrorCode.Should().Be("unauthorized");
    }

    [Fact]
    public async Task SendAsync_Typed_DeserializesResponseBody()
    {
        var handler = new CapturingHandler(HttpStatusCode.OK, """{"Kept":"from-server"}""");

        var result = await Client(handler).GetAsync<Payload>(Url, null);

        result.Kept.Should().Be("from-server");
    }

    private sealed class DisposeTrackingContent(string body) : StringContent(body, Encoding.UTF8, "application/json")
    {
        public bool Disposed { get; private set; }

        protected override void Dispose(bool disposing)
        {
            Disposed = true;
            base.Dispose(disposing);
        }
    }

    private sealed class FixedResponseHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        ) => Task.FromResult(response);
    }

    [Fact]
    public async Task SendAsync_OnSuccess_DisposesResponse()
    {
        var content = new DisposeTrackingContent("""{"Kept":"x"}""");
        var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
        var client = new TestClient(
            new HttpClient(new FixedResponseHandler(response)),
            new JsonSerializerSettings()
        );

        await client.GetAsync<Payload>(Url, null);

        content.Disposed.Should().BeTrue();
    }

    [Fact]
    public async Task SendAsync_OnError_DisposesResponse()
    {
        var content = new DisposeTrackingContent("""{"error":"denied"}""");
        var response = new HttpResponseMessage(HttpStatusCode.Forbidden) { Content = content };
        var client = new TestClient(
            new HttpClient(new FixedResponseHandler(response)),
            new JsonSerializerSettings()
        );

        var act = () => client.GetAsync<Payload>(Url, null);

        await act.Should().ThrowAsync<OpenPaymentsApiException>();
        content.Disposed.Should().BeTrue();
    }
}

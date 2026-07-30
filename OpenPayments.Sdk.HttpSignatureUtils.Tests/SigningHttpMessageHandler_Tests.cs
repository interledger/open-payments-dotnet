using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace OpenPayments.Sdk.HttpSignatureUtils.Tests;

public class SigningHttpMessageHandlerTests
{
    private const string KeyId = "test-key-id";
    private const string Json = "{\"amount\":100}";

    /// <summary>
    /// Captures the request as the inner handler sees it, and reads the body from that
    /// vantage point so the tests can prove signing did not consume the stream.
    /// </summary>
    private sealed class SpyHandler : HttpMessageHandler
    {
        public HttpRequestMessage Request { get; private set; }
        public string Body { get; private set; }
        public int CallCount { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            CallCount++;
            Request = request;
            Body =
                request.Content is null
                    ? null
                    : await request.Content.ReadAsStringAsync();
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }

    // HttpMessageInvoker, not HttpClient: HttpClient marks a request as sent and refuses to
    // send the same HttpRequestMessage twice, which the re-entry test below needs to do.
    private static (HttpMessageInvoker Invoker, SpyHandler Spy) CreateInvoker()
    {
        var spy = new SpyHandler();
        var handler = new SigningHttpMessageHandler(KeyUtils.GenerateKey(), KeyId)
        {
            InnerHandler = spy,
        };
        return (new HttpMessageInvoker(handler), spy);
    }

    private static HttpRequestMessage JsonPost() =>
        new(HttpMethod.Post, "https://example.com/incoming-payments")
        {
            Content = new StringContent(Json, Encoding.UTF8, "application/json"),
        };

    [Fact]
    public async Task SendAsync_WithBody_AddsSignatureHeadersAndLeavesBodyReadable()
    {
        var (invoker, spy) = CreateInvoker();

        await invoker.SendAsync(JsonPost(), CancellationToken.None);

        Assert.Equal(1, spy.CallCount);
        Assert.True(spy.Request.Headers.Contains("Signature"));
        Assert.True(spy.Request.Headers.Contains("Signature-Input"));
        Assert.Equal(Json, spy.Body);
    }

    [Fact]
    public async Task SendAsync_WithBody_ProducesWellFormedSignatureHeaders()
    {
        var (invoker, spy) = CreateInvoker();

        await invoker.SendAsync(JsonPost(), CancellationToken.None);

        var signature = spy.Request.Headers.GetValues("Signature").Single();
        Assert.StartsWith("sig1=:", signature);
        Assert.EndsWith(":", signature);

        var input = spy.Request.Headers.GetValues("Signature-Input").Single();
        Assert.Contains($"keyid=\"{KeyId}\"", input);
        Assert.Contains("alg=\"ed25519\"", input);
        Assert.Contains(
            "(\"@method\" \"@target-uri\" \"content-digest\" \"content-length\" \"content-type\")",
            input
        );
    }

    [Fact]
    public async Task SendAsync_WithBody_AddsContentHeaders()
    {
        var (invoker, spy) = CreateInvoker();

        await invoker.SendAsync(JsonPost(), CancellationToken.None);

        Assert.True(spy.Request.Content.Headers.Contains("Content-Digest"));
        Assert.True(spy.Request.Content.Headers.Contains("Content-Length"));
        Assert.True(spy.Request.Content.Headers.Contains("Content-Type"));
    }

    [Fact]
    public async Task SendAsync_WithoutBody_SignsMethodAndTargetUriOnly()
    {
        var (invoker, spy) = CreateInvoker();
        var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com/wallet");

        await invoker.SendAsync(request, CancellationToken.None);

        var input = spy.Request.Headers.GetValues("Signature-Input").Single();
        Assert.Contains("(\"@method\" \"@target-uri\")", input);
        Assert.DoesNotContain("content-digest", input);
        Assert.Null(spy.Request.Content);
    }

    [Fact]
    public async Task SendAsync_SameRequestSentTwice_DoesNotAccumulateHeaders()
    {
        var (invoker, spy) = CreateInvoker();
        var request = JsonPost();

        await invoker.SendAsync(request, CancellationToken.None);
        await invoker.SendAsync(request, CancellationToken.None);

        Assert.Equal(2, spy.CallCount);
        Assert.Single(spy.Request.Headers.GetValues("Signature"));
        Assert.Single(spy.Request.Headers.GetValues("Signature-Input"));
        Assert.Single(spy.Request.Content.Headers.GetValues("Content-Digest"));
        Assert.Equal(Json, spy.Body);
    }

    [Fact]
    public void Constructor_ThrowsWhenPrivateKeyIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new SigningHttpMessageHandler(null!, KeyId));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ThrowsWhenKeyIdIsBlank(string keyId)
    {
        Assert.Throws<ArgumentException>(() =>
            new SigningHttpMessageHandler(KeyUtils.GenerateKey(), keyId)
        );
    }
}

using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace OpenPayments.Sdk.HttpSignatureUtils.Tests;

public class HttpRequestSignerTests
{
    [Fact]
    public async Task SignHttpRequestAsync_BasicRequest_ReturnsSignatureHeaders()
    {
        var key = KeyUtils.GenerateKey();
        var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com/resource");

        var headers = await HttpRequestSigner.SignHttpRequestAsync(request, key, "test-key-id");

        Assert.NotNull(headers.Signature);
        Assert.NotNull(headers.SignatureInput);
        Assert.StartsWith("sig1=:", headers.Signature);
        Assert.StartsWith("sig1=(", headers.SignatureInput);
    }

    [Fact]
    public async Task SignHttpRequestAsync_WithAuthorizationHeader_IncludesAuthorizationComponent()
    {
        var key = KeyUtils.GenerateKey();
        var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com/auth")
        {
            Headers = { { "Authorization", "Bearer abc123" } },
        };

        var headers = await HttpRequestSigner.SignHttpRequestAsync(request, key, "auth-key");

        Assert.Contains("\"authorization\"", headers.SignatureInput);
        Assert.Contains("sig1=:", headers.Signature);
    }

    [Fact]
    public async Task SignHttpRequestAsync_WithBody_AddsContentHeaders()
    {
        var key = KeyUtils.GenerateKey();
        var contentJson = "{\"amount\":100}";
        var request = new HttpRequestMessage(HttpMethod.Post, "https://example.com/pay")
        {
            Content = new StringContent(contentJson, Encoding.UTF8, "application/json"),
        };

        var headers = await HttpRequestSigner.SignHttpRequestAsync(request, key, "body-key");

        Assert.Contains("\"content-digest\"", headers.SignatureInput);
        Assert.Contains("\"content-length\"", headers.SignatureInput);
        Assert.Contains("\"content-type\"", headers.SignatureInput);

        Assert.True(request.Content.Headers.Contains("Content-Digest"));
        Assert.True(request.Content.Headers.Contains("Content-Length"));
        Assert.True(request.Content.Headers.Contains("Content-Type"));
    }

    [Fact]
    public async Task SignHttpRequestAsync_ThrowsIfKeyIsNull()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com");

        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await HttpRequestSigner.SignHttpRequestAsync(request, null!, "key-id")
        );
    }

    [Fact]
    public async Task SignHttpRequestAsync_ThrowsIfKeyIdIsEmpty()
    {
        var key = KeyUtils.GenerateKey();
        var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com");

        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await HttpRequestSigner.SignHttpRequestAsync(request, key, "")
        );
    }

    [Fact]
    public async Task SignHttpRequestAsync_WithStreamContent_ComputesDigestAndLeavesBodyReadable()
    {
        var key = KeyUtils.GenerateKey();
        var json = "{\"amount\":100}";
        var bytes = Encoding.UTF8.GetBytes(json);
        var request = new HttpRequestMessage(HttpMethod.Post, "https://example.com/pay")
        {
            Content = new StreamContent(new MemoryStream(bytes)),
        };

        var headers = await HttpRequestSigner.SignHttpRequestAsync(request, key, "stream-key");

        var expectedDigest = Convert.ToBase64String(SHA512.HashData(bytes));
        var digest = request.Content.Headers.GetValues("Content-Digest").Single();
        Assert.Equal($"sha-512=:{expectedDigest}:", digest);

        Assert.Contains("\"content-digest\"", headers.SignatureInput);
        Assert.Equal(json, await request.Content.ReadAsStringAsync());
    }
}

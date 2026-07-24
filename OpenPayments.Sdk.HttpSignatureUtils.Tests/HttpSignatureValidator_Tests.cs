using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using NSec.Cryptography;
using Xunit;

namespace Interledger.OpenPayments.HttpSignatureUtils.Tests;

public class HttpSignatureValidator_Tests
{
    private static HttpSignatureValidator CreateValidator() =>
        new(new SignatureInputParser(), new SignatureInputValidator(), new SignatureInputBuilder());

    private static async Task<HttpRequestMessage> SignAsync(
        HttpRequestMessage request,
        Key key,
        string keyId
    )
    {
        var headers = await HttpRequestSigner.SignHttpRequestAsync(request, key, keyId);
        request.Headers.TryAddWithoutValidation("Signature", headers.Signature);
        request.Headers.TryAddWithoutValidation("Signature-Input", headers.SignatureInput);
        return request;
    }

    [Fact]
    public void AreSignatureHeadersPresent_MissingHeaders_ReturnsFalse()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com/resource");
        Assert.False(CreateValidator().AreSignatureHeadersPresent(request));
    }

    [Fact]
    public async Task AreSignatureHeadersPresent_AfterSigning_ReturnsTrue()
    {
        var key = KeyUtils.GenerateKey();
        var request = await SignAsync(
            new HttpRequestMessage(HttpMethod.Get, "https://example.com/resource"),
            key,
            "test-key"
        );

        Assert.True(CreateValidator().AreSignatureHeadersPresent(request));
    }

    [Fact]
    public async Task ValidateSignatureAsync_SignedGetRequest_ReturnsTrue()
    {
        var key = KeyUtils.GenerateKey();
        var jwk = KeyUtils.GenerateJwk("test-key", key);
        var request = await SignAsync(
            new HttpRequestMessage(HttpMethod.Get, "https://example.com/resource"),
            key,
            "test-key"
        );

        Assert.True(await CreateValidator().ValidateSignatureAsync(request, jwk));
    }

    [Fact]
    public async Task ValidateSignatureAsync_SignedRequestWithAuthorizationAndBody_ReturnsTrue()
    {
        var key = KeyUtils.GenerateKey();
        var jwk = KeyUtils.GenerateJwk("test-key", key);
        var request = new HttpRequestMessage(HttpMethod.Post, "https://example.com/grant")
        {
            Content = new StringContent(
                "{\"access_token\":{}}",
                Encoding.UTF8,
                "application/json"
            ),
        };
        request.Headers.TryAddWithoutValidation("Authorization", "GNAP token-123");
        await SignAsync(request, key, "test-key");

        Assert.True(await CreateValidator().ValidateSignatureAsync(request, jwk));
    }

    [Fact]
    public async Task ValidateSignatureAsync_HeaderTamperedAfterSigning_ReturnsFalse()
    {
        var key = KeyUtils.GenerateKey();
        var jwk = KeyUtils.GenerateJwk("test-key", key);
        var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com/resource");
        request.Headers.TryAddWithoutValidation("Authorization", "GNAP token-123");
        await SignAsync(request, key, "test-key");

        request.Headers.Remove("Authorization");
        request.Headers.TryAddWithoutValidation("Authorization", "GNAP tampered");

        Assert.False(await CreateValidator().ValidateSignatureAsync(request, jwk));
    }

    [Fact]
    public async Task ValidateSignatureAsync_WrongPublicKey_ReturnsFalse()
    {
        var signingKey = KeyUtils.GenerateKey();
        var otherJwk = KeyUtils.GenerateJwk("other-key");
        var request = await SignAsync(
            new HttpRequestMessage(HttpMethod.Get, "https://example.com/resource"),
            signingKey,
            "test-key"
        );

        Assert.False(await CreateValidator().ValidateSignatureAsync(request, otherJwk));
    }
}

using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using NSec.Cryptography;
using Xunit;

namespace OpenPayments.Sdk.HttpSignatureUtils.Tests;

public class HttpSignatureValidatorRoundTripTests
{
    private static HttpSignatureValidator NewValidator() =>
        new(new SignatureInputParser(), new SignatureInputValidator(), new SignatureInputBuilder());

    /// <summary>
    /// Signs the request and copies the returned headers onto it, the way a real client does.
    /// SignHttpRequestAsync returns the headers rather than setting them.
    /// </summary>
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
    public async Task ValidateSignatureAsync_GetSignedByThisLibrary_IsValid()
    {
        var key = KeyUtils.GenerateKey();
        var jwk = KeyUtils.GenerateJwk("round-trip-key", key);
        var request = await SignAsync(
            new HttpRequestMessage(HttpMethod.Get, "https://example.com/resource"),
            key,
            "round-trip-key"
        );

        Assert.True(await NewValidator().ValidateSignatureAsync(request, jwk));
    }

    [Fact]
    public async Task ValidateSignatureAsync_PostWithBodySignedByThisLibrary_IsValid()
    {
        var key = KeyUtils.GenerateKey();
        var jwk = KeyUtils.GenerateJwk("round-trip-key", key);
        var request = await SignAsync(
            new HttpRequestMessage(HttpMethod.Post, "https://example.com/pay")
            {
                Content = new StringContent("{\"amount\":100}", Encoding.UTF8, "application/json"),
            },
            key,
            "round-trip-key"
        );

        Assert.True(await NewValidator().ValidateSignatureAsync(request, jwk));
    }

    [Fact]
    public async Task ValidateSignatureAsync_GetWithAuthorizationHeader_IsValid()
    {
        var key = KeyUtils.GenerateKey();
        var jwk = KeyUtils.GenerateJwk("round-trip-key", key);
        var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com/resource");
        request.Headers.TryAddWithoutValidation("Authorization", "GNAP some-token");

        await SignAsync(request, key, "round-trip-key");

        Assert.Contains("\"authorization\"", request.Headers.GetValues("Signature-Input").Single());
        Assert.True(await NewValidator().ValidateSignatureAsync(request, jwk));
    }

    [Fact]
    public async Task ValidateSignatureAsync_WrongKey_IsInvalid()
    {
        var key = KeyUtils.GenerateKey();
        var otherJwk = KeyUtils.GenerateJwk("round-trip-key", KeyUtils.GenerateKey());
        var request = await SignAsync(
            new HttpRequestMessage(HttpMethod.Get, "https://example.com/resource"),
            key,
            "round-trip-key"
        );

        Assert.False(await NewValidator().ValidateSignatureAsync(request, otherJwk));
    }

    [Fact]
    public async Task AreSignatureHeadersPresent_AfterSigning_IsTrue()
    {
        var key = KeyUtils.GenerateKey();
        var request = await SignAsync(
            new HttpRequestMessage(HttpMethod.Get, "https://example.com/resource"),
            key,
            "round-trip-key"
        );

        Assert.True(NewValidator().AreSignatureHeadersPresent(request));
    }

    [Fact]
    public void AreSignatureHeadersPresent_UnsignedRequest_IsFalse()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com/resource");

        Assert.False(NewValidator().AreSignatureHeadersPresent(request));
    }
}

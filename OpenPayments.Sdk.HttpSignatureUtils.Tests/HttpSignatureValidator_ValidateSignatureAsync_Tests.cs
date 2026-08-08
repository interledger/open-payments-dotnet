using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using NSec.Cryptography;
using OpenPayments.Sdk.HttpSignatureUtils;
using Xunit;

namespace OpenPayments.Sdk.HttpSignatureUtils.Tests;

public class HttpSignatureValidator_ValidateSignatureAsync_Tests
{
    private static HttpSignatureValidator CreateValidator() =>
        new(new SignatureInputParser(), new SignatureInputValidator(), new SignatureInputBuilder());

    private static (Key key, Jwk jwk) CreateKeyPair(string keyId = "test-key")
    {
        var key = KeyUtils.GenerateKey();
        var jwk = KeyUtils.GenerateJwk(keyId, key);
        return (key, jwk);
    }

    private static async Task AttachSignatureAsync(
        HttpRequestMessage request,
        Key privateKey,
        string keyId
    )
    {
        var headers = await HttpRequestSigner.SignHttpRequestAsync(request, privateKey, keyId);
        request.Headers.TryAddWithoutValidation("Signature", headers.Signature);
        request.Headers.TryAddWithoutValidation("Signature-Input", headers.SignatureInput);
    }

    [Fact]
    public async Task ValidateSignatureAsync_AcceptsMatchingBodyAndDigest()
    {
        var (key, jwk) = CreateKeyPair();
        var body = """{"amount":"1"}""";
        var request = new HttpRequestMessage(HttpMethod.Post, "https://example.com/pay")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
        await AttachSignatureAsync(request, key, "test-key");

        var ok = await CreateValidator().ValidateSignatureAsync(request, jwk);
        Assert.True(ok);
    }

    [Fact]
    public async Task ValidateSignatureAsync_RejectsBodySwapUnderStaleContentDigest()
    {
        var (key, jwk) = CreateKeyPair();
        var originalBody = """{"amount":"1"}""";
        var request = new HttpRequestMessage(HttpMethod.Post, "https://example.com/pay")
        {
            Content = new StringContent(originalBody, Encoding.UTF8, "application/json"),
        };
        await AttachSignatureAsync(request, key, "test-key");

        var staleDigest = request.Content!.Headers.GetValues("Content-Digest").First();
        var signature = request.Headers.GetValues("Signature").First();
        var signatureInput = request.Headers.GetValues("Signature-Input").First();

        // Swap body but keep Signature, Signature-Input, and original Content-Digest.
        // Ed25519 over the signature base still verifies; digest-vs-body must fail closed.
        var swapped = new HttpRequestMessage(HttpMethod.Post, "https://example.com/pay")
        {
            Content = new StringContent(
                """{"amount":"999999"}""",
                Encoding.UTF8,
                "application/json"
            ),
        };
        swapped.Content.Headers.TryAddWithoutValidation("Content-Digest", staleDigest);
        swapped.Headers.TryAddWithoutValidation("Signature", signature);
        swapped.Headers.TryAddWithoutValidation("Signature-Input", signatureInput);

        var ok = await CreateValidator().ValidateSignatureAsync(swapped, jwk);
        Assert.False(ok);
    }

    [Fact]
    public async Task ContentDigest_MatchesRequest_RejectsTamperedBody()
    {
        var body = """{"amount":"1"}""";
        var request = new HttpRequestMessage(HttpMethod.Post, "https://example.com/pay")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
        request.Content.Headers.TryAddWithoutValidation(
            "Content-Digest",
            ContentDigest.ForBody(body)
        );
        Assert.True(await ContentDigest.MatchesRequestAsync(request));

        request.Content = new StringContent(
            """{"amount":"2"}""",
            Encoding.UTF8,
            "application/json"
        );
        request.Content.Headers.TryAddWithoutValidation(
            "Content-Digest",
            ContentDigest.ForBody(body)
        );
        Assert.False(await ContentDigest.MatchesRequestAsync(request));
    }
}

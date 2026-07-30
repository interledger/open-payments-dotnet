using System;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
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

    [Theory]
    [InlineData("sig1=:not-valid-base64!!:")]
    [InlineData("sig1=:")]
    [InlineData("sig1=no-colons-at-all")]
    [InlineData("garbage")]
    public async Task ValidateSignatureAsync_MalformedSignatureHeader_ReturnsFalseWithoutThrowing(
        string malformedSignature
    )
    {
        var key = KeyUtils.GenerateKey();
        var jwk = KeyUtils.GenerateJwk("round-trip-key", key);
        var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com/resource");
        var headers = await HttpRequestSigner.SignHttpRequestAsync(request, key, "round-trip-key");

        request.Headers.TryAddWithoutValidation("Signature", malformedSignature);
        request.Headers.TryAddWithoutValidation("Signature-Input", headers.SignatureInput);

        Assert.False(await NewValidator().ValidateSignatureAsync(request, jwk));
    }

    [Fact]
    public async Task ValidateSignatureAsync_BodySwappedWithReplayedContentHeaders_IsInvalid()
    {
        var key = KeyUtils.GenerateKey();
        var jwk = KeyUtils.GenerateJwk("round-trip-key", key);
        var signed = new HttpRequestMessage(HttpMethod.Post, "https://example.com/pay")
        {
            Content = new StringContent("{\"amount\":100}", Encoding.UTF8, "application/json"),
        };
        var headers = await HttpRequestSigner.SignHttpRequestAsync(signed, key, "round-trip-key");

        // An attacker body carrying the ORIGINAL signed content headers. The base string is
        // byte-identical, so only a digest-versus-body check can reject this.
        var tampered = new HttpRequestMessage(HttpMethod.Post, "https://example.com/pay")
        {
            Content = new StringContent("{\"amount\":999}", Encoding.UTF8, "application/json"),
        };
        tampered.Content.Headers.Clear();
        foreach (var header in signed.Content!.Headers)
            tampered.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);

        tampered.Headers.TryAddWithoutValidation("Signature", headers.Signature);
        tampered.Headers.TryAddWithoutValidation("Signature-Input", headers.SignatureInput);

        Assert.False(await NewValidator().ValidateSignatureAsync(tampered, jwk));
    }

    [Fact]
    public async Task ValidateSignatureAsync_ContentReplacedAfterSigning_IsInvalid()
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

        // A distinct rejection path from the test above: the replacement content has no
        // Content-Digest header, so SignatureInputValidator rejects it before any digest check.
        request.Content = new StringContent("{\"amount\":999}", Encoding.UTF8, "application/json");

        Assert.False(await NewValidator().ValidateSignatureAsync(request, jwk));
    }

    [Fact]
    public async Task ValidateSignatureAsync_DigestSplitBetweenRequestAndContentHeaders_IsInvalid()
    {
        // Final-review Finding 1: SignatureBaseBuilder.GetHeaderValue (what the signature actually
        // commits to) resolves request headers before content headers. If ContentDigestVerifier
        // instead reads content headers directly, an attacker can carry the ORIGINAL digest on a
        // request header (satisfying the signature) while carrying a digest for a NEW body on the
        // content header (satisfying a verifier that reads only content headers), forging an
        // arbitrary body under a valid signature.
        var key = KeyUtils.GenerateKey();
        var jwk = KeyUtils.GenerateJwk("round-trip-key", key);
        // Same length/content-type as the forged body so content-length and content-type (also
        // covered components) resolve identically either way, isolating the digest-source bug: only
        // content-digest differs between the request-header and content-header values.
        const string originalBody = "{\"amount\":100000}";
        const string forgedBody = "{\"amount\":999999}";

        var signed = new HttpRequestMessage(HttpMethod.Post, "https://example.com/pay")
        {
            Content = new StringContent(originalBody, Encoding.UTF8, "application/json"),
        };
        var headers = await HttpRequestSigner.SignHttpRequestAsync(signed, key, "round-trip-key");
        var originalDigest = signed.Content.Headers.GetValues("Content-Digest").Single();

        var forgedDigest =
            $"sha-512=:{Convert.ToBase64String(SHA512.HashData(Encoding.UTF8.GetBytes(forgedBody)))}:";

        var tampered = new HttpRequestMessage(HttpMethod.Post, "https://example.com/pay")
        {
            Content = new StringContent(forgedBody, Encoding.UTF8, "application/json"),
        };
        // Content headers describe the NEW (forged) body, so ContentDigestVerifier reading content
        // headers directly would accept it. Content-Length is added explicitly because StringContent
        // does not populate it as a raw header (only lazily via the ContentLength property), and
        // SignatureInputValidator requires it present alongside Content-Digest/-Type.
        tampered.Content.Headers.TryAddWithoutValidation("Content-Digest", forgedDigest);
        tampered.Content.Headers.TryAddWithoutValidation(
            "Content-Length",
            Encoding.UTF8.GetByteCount(forgedBody).ToString()
        );
        // Request headers carry the ORIGINAL digest, so SignatureBaseBuilder.GetHeaderValue (request
        // headers checked first) reproduces the signed base string unchanged.
        tampered.Headers.TryAddWithoutValidation("Content-Digest", originalDigest);
        tampered.Headers.TryAddWithoutValidation("Signature", headers.Signature);
        tampered.Headers.TryAddWithoutValidation("Signature-Input", headers.SignatureInput);

        Assert.False(await NewValidator().ValidateSignatureAsync(tampered, jwk));
    }

    [Fact]
    public async Task ValidateSignatureAsync_BodyAttachedToBodylessSignedRequest_IsInvalid()
    {
        // Final-review Finding 2: HttpRequestSigner only covers content-digest/-length/-type when the
        // body was non-empty at signing time. A bodyless signature (covering only @method and
        // @target-uri) must not validate once an attacker attaches an arbitrary body afterward.
        var key = KeyUtils.GenerateKey();
        var jwk = KeyUtils.GenerateJwk("round-trip-key", key);
        var bodyless = new HttpRequestMessage(HttpMethod.Post, "https://example.com/pay");
        var headers = await HttpRequestSigner.SignHttpRequestAsync(bodyless, key, "round-trip-key");

        Assert.DoesNotContain("content-digest", headers.SignatureInput);

        var withAttachedBody = new HttpRequestMessage(HttpMethod.Post, "https://example.com/pay")
        {
            Content = new StringContent("{\"amount\":999999}", Encoding.UTF8, "application/json"),
        };
        withAttachedBody.Headers.TryAddWithoutValidation("Signature", headers.Signature);
        withAttachedBody.Headers.TryAddWithoutValidation("Signature-Input", headers.SignatureInput);

        Assert.False(await NewValidator().ValidateSignatureAsync(withAttachedBody, jwk));
    }

    [Fact]
    public async Task ValidateSignatureAsync_SignatureHeaderMissing_ReturnsFalseWithoutThrowing()
    {
        // Final-review Finding 3: Signature-Input present but Signature absent must return false,
        // not throw NullReferenceException.
        var key = KeyUtils.GenerateKey();
        var jwk = KeyUtils.GenerateJwk("round-trip-key", key);
        var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com/resource");
        var headers = await HttpRequestSigner.SignHttpRequestAsync(request, key, "round-trip-key");
        request.Headers.TryAddWithoutValidation("Signature-Input", headers.SignatureInput);

        Assert.False(await NewValidator().ValidateSignatureAsync(request, jwk));
    }

    [Fact]
    public async Task ValidateSignatureAsync_SignatureInputHeaderMissing_ReturnsFalseWithoutThrowing()
    {
        var key = KeyUtils.GenerateKey();
        var jwk = KeyUtils.GenerateJwk("round-trip-key", key);
        var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com/resource");
        var headers = await HttpRequestSigner.SignHttpRequestAsync(request, key, "round-trip-key");
        request.Headers.TryAddWithoutValidation("Signature", headers.Signature);

        Assert.False(await NewValidator().ValidateSignatureAsync(request, jwk));
    }
}

#nullable enable

using System;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace OpenPayments.Sdk.HttpSignatureUtils.Tests;

public class ContentDigestVerifierTests
{
    private const string Body = "{\"amount\":100}";

    private static HttpRequestMessage RequestWithDigest(string? digestHeader, string body = Body)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "https://example.com/pay")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };

        if (digestHeader is not null)
            request.Content.Headers.TryAddWithoutValidation("Content-Digest", digestHeader);

        return request;
    }

    private static string Sha512Of(string body) =>
        Convert.ToBase64String(SHA512.HashData(Encoding.UTF8.GetBytes(body)));

    private static string Sha256Of(string body) =>
        Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(body)));

    [Fact]
    public async Task MatchesBodyAsync_MatchingSha512_IsTrue()
    {
        var request = RequestWithDigest($"sha-512=:{Sha512Of(Body)}:");

        Assert.True(await ContentDigestVerifier.MatchesBodyAsync(request));
    }

    [Fact]
    public async Task MatchesBodyAsync_MatchingSha256_IsTrue()
    {
        var request = RequestWithDigest($"sha-256=:{Sha256Of(Body)}:");

        Assert.True(await ContentDigestVerifier.MatchesBodyAsync(request));
    }

    [Fact]
    public async Task MatchesBodyAsync_DigestOfADifferentBody_IsFalse()
    {
        var request = RequestWithDigest($"sha-512=:{Sha512Of("{\"amount\":999}")}:");

        Assert.False(await ContentDigestVerifier.MatchesBodyAsync(request));
    }

    [Fact]
    public async Task MatchesBodyAsync_BothAlgorithmsMatching_IsTrue()
    {
        var request = RequestWithDigest(
            $"sha-256=:{Sha256Of(Body)}:, sha-512=:{Sha512Of(Body)}:"
        );

        Assert.True(await ContentDigestVerifier.MatchesBodyAsync(request));
    }

    [Fact]
    public async Task MatchesBodyAsync_OneAlgorithmMismatched_IsFalse()
    {
        var request = RequestWithDigest(
            $"sha-256=:{Sha256Of("other")}:, sha-512=:{Sha512Of(Body)}:"
        );

        Assert.False(await ContentDigestVerifier.MatchesBodyAsync(request));
    }

    [Fact]
    public async Task MatchesBodyAsync_UnrecognisedAlgorithmOnly_IsFalse()
    {
        // Fail closed: asked to verify a digest and unable to is a rejection, not a pass.
        var request = RequestWithDigest("sha-1=:AAAA:");

        Assert.False(await ContentDigestVerifier.MatchesBodyAsync(request));
    }

    [Fact]
    public async Task MatchesBodyAsync_NoDigestHeader_IsFalse()
    {
        var request = RequestWithDigest(null);

        Assert.False(await ContentDigestVerifier.MatchesBodyAsync(request));
    }

    [Fact]
    public async Task MatchesBodyAsync_NoContent_IsFalse()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com/r");

        Assert.False(await ContentDigestVerifier.MatchesBodyAsync(request));
    }

    [Fact]
    public async Task MatchesBodyAsync_MalformedHeader_IsFalse()
    {
        var request = RequestWithDigest("this-is-not-a-digest");

        Assert.False(await ContentDigestVerifier.MatchesBodyAsync(request));
    }

    [Fact]
    public async Task MatchesBodyAsync_DigestProducedByTheSigner_IsTrue()
    {
        // Locks the verifier to the signer's exact digest computation (HttpRequestSigner.cs:37).
        var key = KeyUtils.GenerateKey();
        var request = new HttpRequestMessage(HttpMethod.Post, "https://example.com/pay")
        {
            Content = new StringContent(Body, Encoding.UTF8, "application/json"),
        };

        await HttpRequestSigner.SignHttpRequestAsync(request, key, "digest-key");

        Assert.True(await ContentDigestVerifier.MatchesBodyAsync(request));
    }
}

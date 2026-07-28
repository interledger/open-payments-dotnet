using System;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace OpenPayments.Sdk.HttpSignatureUtils.Tests;

public class SignatureInputBuilder_Tests
{
    private readonly SignatureInputBuilder _builder = new();

    [Fact]
    public async Task BuildBaseAsync_Get_UsesUppercaseMethodAndLineFeedSeparators()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com/resource");
        var sigInput =
            "sig1=(\"@method\" \"@target-uri\");created=1700000000;keyid=\"test-key\";alg=\"ed25519\"";

        var result = await _builder.BuildBaseAsync(["@method", "@target-uri"], request, sigInput);

        var expected =
            "\"@method\": GET\n"
            + "\"@target-uri\": https://example.com/resource\n"
            + "\"@signature-params\": (\"@method\" \"@target-uri\");created=1700000000;keyid=\"test-key\";alg=\"ed25519\"";
        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task BuildBaseAsync_HeaderComponent_UsesHeaderValue()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com/resource");
        request.Headers.TryAddWithoutValidation("authorization", "GNAP token-123");
        var sigInput =
            "sig1=(\"@method\" \"@target-uri\" \"authorization\");created=1700000000;keyid=\"test-key\";alg=\"ed25519\"";

        var result = await _builder.BuildBaseAsync(
            ["@method", "@target-uri", "authorization"],
            request,
            sigInput
        );

        Assert.Contains("\"authorization\": GNAP token-123\n", result);
    }

    [Fact]
    public async Task BuildBaseAsync_ContentDigestFallback_ComputesSha512LikeTheSigner()
    {
        var body = "{\"access_token\":{}}";
        var request = new HttpRequestMessage(HttpMethod.Post, "https://example.com/resource")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
        var sigInput =
            "sig1=(\"content-digest\");created=1700000000;keyid=\"test-key\";alg=\"ed25519\"";

        var result = await _builder.BuildBaseAsync(["content-digest"], request, sigInput);

        var expectedDigest = Convert.ToBase64String(SHA512.HashData(Encoding.UTF8.GetBytes(body)));
        Assert.Contains($"\"content-digest\": sha-512=:{expectedDigest}:\n", result);
    }
}

using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using Xunit;

namespace Interledger.OpenPayments.HttpSignatureUtils.Tests;

public class SignatureInputValidator_Tests
{
    private readonly SignatureInputValidator _validator = new();

    private static HttpRequestMessage Get() =>
        new(HttpMethod.Get, "https://example.com/incoming-payments");

    [Fact]
    public void Validate_MethodAndTargetUri_NoAuthNoBody_ReturnsTrue()
    {
        Assert.True(_validator.Validate(["@method", "@target-uri"], Get()));
    }

    [Fact]
    public void Validate_MissingMethod_ReturnsFalse()
    {
        Assert.False(_validator.Validate(["@target-uri"], Get()));
    }

    [Fact]
    public void Validate_MissingTargetUri_ReturnsFalse()
    {
        Assert.False(_validator.Validate(["@method"], Get()));
    }

    [Fact]
    public void Validate_AuthorizationHeaderPresentButNotCovered_ReturnsFalse()
    {
        var request = Get();
        request.Headers.TryAddWithoutValidation("Authorization", "GNAP token-123");

        Assert.False(_validator.Validate(["@method", "@target-uri"], request));
    }

    [Fact]
    public void Validate_AuthorizationHeaderCovered_ReturnsTrue()
    {
        var request = Get();
        request.Headers.TryAddWithoutValidation("Authorization", "GNAP token-123");

        Assert.True(_validator.Validate(["@method", "@target-uri", "authorization"], request));
    }

    [Fact]
    public void Validate_ContentDigestCoveredButNoContent_ReturnsFalse()
    {
        Assert.False(_validator.Validate(["@method", "@target-uri", "content-digest"], Get()));
    }

    [Fact]
    public void Validate_ContentDigestCoveredWithAllContentHeaders_ReturnsTrue()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "https://example.com/x")
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json"),
        };
        request.Content.Headers.TryAddWithoutValidation("Content-Digest", "sha-512=:abc:");
        request.Content.Headers.ContentLength = 2;

        Assert.True(_validator.Validate(["@method", "@target-uri", "content-digest"], request));
    }
}

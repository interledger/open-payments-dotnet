using System.Net.Http;
using System.Text;
using Xunit;

namespace OpenPayments.Sdk.HttpSignatureUtils.Tests;

public class SignatureInputValidatorTests
{
    [Fact]
    public void Validate_MixedCaseComponent_IsRejected()
    {
        var validator = new SignatureInputValidator();
        var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com/r");

        // RFC 9421 section 2.1 requires lowercase component names. The original check compared a
        // string to itself, so it accepted anything.
        Assert.False(
            validator.Validate(["@method", "@target-uri", "Content-Type"], request)
        );
    }

    [Fact]
    public void Validate_MinimalLowercaseComponents_IsAccepted()
    {
        var validator = new SignatureInputValidator();
        var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com/r");

        Assert.True(validator.Validate(["@method", "@target-uri"], request));
    }

    [Fact]
    public void Validate_MissingMethod_IsRejected()
    {
        var validator = new SignatureInputValidator();
        var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com/r");

        Assert.False(validator.Validate(["@target-uri"], request));
    }

    [Fact]
    public void Validate_MissingTargetUri_IsRejected()
    {
        var validator = new SignatureInputValidator();
        var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com/r");

        Assert.False(validator.Validate(["@method"], request));
    }

    [Fact]
    public void Validate_AuthorizationHeaderNotCovered_IsRejected()
    {
        var validator = new SignatureInputValidator();
        var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com/r");
        request.Headers.TryAddWithoutValidation("Authorization", "GNAP token");

        Assert.False(validator.Validate(["@method", "@target-uri"], request));
    }

    [Fact]
    public void Validate_AuthorizationHeaderCovered_IsAccepted()
    {
        var validator = new SignatureInputValidator();
        var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com/r");
        request.Headers.TryAddWithoutValidation("Authorization", "GNAP token");

        Assert.True(
            validator.Validate(["@method", "@target-uri", "authorization"], request)
        );
    }

    [Fact]
    public void Validate_ContentDigestCoveredButHeaderAbsent_IsRejected()
    {
        var validator = new SignatureInputValidator();
        var request = new HttpRequestMessage(HttpMethod.Post, "https://example.com/pay")
        {
            Content = new StringContent("{\"amount\":100}", Encoding.UTF8, "application/json"),
        };

        Assert.False(
            validator.Validate(["@method", "@target-uri", "content-digest"], request)
        );
    }

    [Fact]
    public void Validate_ContentDigestCoveredWithAllContentHeaders_IsAccepted()
    {
        var validator = new SignatureInputValidator();
        var request = new HttpRequestMessage(HttpMethod.Post, "https://example.com/pay")
        {
            Content = new StringContent("{\"amount\":100}", Encoding.UTF8, "application/json"),
        };
        request.Content.Headers.TryAddWithoutValidation("Content-Digest", "sha-512=:AAA:");

        // Preconditions asserted so a failure here is self-diagnosing: StringContent supplies
        // Content-Type at construction; accessing ContentLength triggers Content-Length computation.
        Assert.True(request.Content.Headers.Contains("Content-Type"));
        _ = request.Content.Headers.ContentLength;
        Assert.True(request.Content.Headers.Contains("Content-Length"));

        Assert.True(
            validator.Validate(["@method", "@target-uri", "content-digest"], request)
        );
    }
}

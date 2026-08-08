using OpenPayments.Sdk.HttpSignatureUtils;
using Xunit;

namespace OpenPayments.Sdk.HttpSignatureUtils.Tests;

public class SignatureInputValidator_Validate_Tests
{
    private readonly SignatureInputValidator _validator = new();

    [Fact]
    public void Validate_RejectsMissingContentDigestCoverageWhenBodyPresent()
    {
        var body = """{"amount":"1"}""";
        var request = new HttpRequestMessage(HttpMethod.Post, "https://example.com/resource")
        {
            Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json"),
        };
        request.Content.Headers.ContentLength = body.Length;

        // Cover method/target/type but omit content-digest despite body.
        var components = new List<string> { "@method", "@target-uri", "content-type" };

        Assert.False(_validator.Validate(components, request));
    }

    [Fact]
    public void Validate_AcceptsContentDigestCoverageWhenBodyPresent()
    {
        var body = """{"amount":"1"}""";
        var request = new HttpRequestMessage(HttpMethod.Post, "https://example.com/resource")
        {
            Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json"),
        };
        request.Content.Headers.TryAddWithoutValidation(
            "Content-Digest",
            "sha-512=:AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA==:"
        );
        // StringContent already sets Content-Type; ensure Content-Length is present.
        request.Content.Headers.ContentLength = body.Length;

        var components = new List<string>
        {
            "@method",
            "@target-uri",
            "content-digest",
            "content-length",
            "content-type",
        };

        Assert.True(_validator.Validate(components, request));
    }

    [Fact]
    public void Validate_AllowsMissingContentDigestWhenNoBody()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com/resource");
        var components = new List<string> { "@method", "@target-uri" };

        Assert.True(_validator.Validate(components, request));
    }

    [Fact]
    public void Validate_RejectsNonLowercaseComponents()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com/resource");
        var components = new List<string> { "@METHOD", "@target-uri" };

        Assert.False(_validator.Validate(components, request));
    }
}

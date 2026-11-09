using System.Net.Http;
using System.Text;
using Xunit;

namespace OpenPayments.Sdk.HttpSignatureUtils.Tests;

public class SignatureBaseBuilderTests
{
    private const string Params =
        "(\"@method\" \"@target-uri\");created=1700000000;keyid=\"test-key\";alg=\"ed25519\"";

    [Fact]
    public void Build_UsesLfSeparatorsOnly()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com/resource");

        var result = SignatureBaseBuilder.Build(["@method", "@target-uri"], Params, request);

        // Guards the Environment.NewLine regression. CI is ubuntu-only, where AppendLine emits LF
        // anyway, so this assertion is the only thing that catches it.
        Assert.DoesNotContain("\r", result);
        Assert.Equal(
            "\"@method\": GET\n"
                + "\"@target-uri\": https://example.com/resource\n"
                + "\"@signature-params\": "
                + Params,
            result
        );
    }

    [Fact]
    public void Build_UppercasesMethod()
    {
        // new HttpMethod("get").Method returns "get" verbatim, so this is a real test.
        var request = new HttpRequestMessage(new HttpMethod("get"), "https://example.com/r");

        var result = SignatureBaseBuilder.Build(["@method"], Params, request);

        Assert.StartsWith("\"@method\": GET\n", result);
    }

    [Fact]
    public void Build_LowercasesHeaderComponentName()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "https://example.com/pay")
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json"),
        };

        var result = SignatureBaseBuilder.Build(["Content-Type"], Params, request);

        Assert.StartsWith("\"content-type\": application/json; charset=utf-8\n", result);
    }

    [Fact]
    public void Build_ReadsRequestHeadersAndContentHeaders()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "https://example.com/pay")
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json"),
        };
        request.Headers.TryAddWithoutValidation("Authorization", "GNAP token");

        var result = SignatureBaseBuilder.Build(
            ["authorization", "content-type"],
            Params,
            request
        );

        Assert.StartsWith(
            "\"authorization\": GNAP token\n\"content-type\": application/json; charset=utf-8\n",
            result
        );
    }

    [Fact]
    public void Build_EmitsEmptyValueForAbsentHeader()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com/r");

        var result = SignatureBaseBuilder.Build(["x-absent"], Params, request);

        Assert.StartsWith("\"x-absent\": \n", result);
    }

    [Fact]
    public void Build_JoinsRepeatedHeaderValuesWithCommaSpace()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com/r");
        request.Headers.TryAddWithoutValidation("X-Multi", "a");
        request.Headers.TryAddWithoutValidation("X-Multi", "b");

        var result = SignatureBaseBuilder.Build(["x-multi"], Params, request);

        Assert.StartsWith("\"x-multi\": a, b\n", result);
    }

    [Fact]
    public void Build_AppendsSignatureParamsAsFinalLine()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com/r");

        var result = SignatureBaseBuilder.Build(["@method"], Params, request);

        Assert.EndsWith("\n\"@signature-params\": " + Params, result);
    }

    [Fact]
    public void Build_DoesNotComputeContentDigestWhenHeaderIsAbsent()
    {
        // The fallback digest computation is deliberately gone: the signer sets the header before
        // building, and the validator rejects a covered content-digest with no header.
        var request = new HttpRequestMessage(HttpMethod.Post, "https://example.com/pay")
        {
            Content = new StringContent("{\"amount\":100}", Encoding.UTF8, "application/json"),
        };

        var result = SignatureBaseBuilder.Build(["content-digest"], Params, request);

        Assert.StartsWith("\"content-digest\": \n", result);
    }
}

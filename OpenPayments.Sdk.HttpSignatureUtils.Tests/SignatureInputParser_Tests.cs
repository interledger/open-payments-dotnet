using System.Collections.Generic;
using Xunit;

namespace OpenPayments.Sdk.HttpSignatureUtils.Tests;

public class SignatureInputParser_Tests
{
    private readonly SignatureInputParser _parser = new();

    [Fact]
    public void GetComponents_MinimalGetInput_ReturnsMethodAndTargetUri()
    {
        var sigInput =
            "sig1=(\"@method\" \"@target-uri\");created=1700000000;keyid=\"test-key\";alg=\"ed25519\"";

        var components = _parser.GetComponents(sigInput);

        Assert.NotNull(components);
        Assert.Equal(new List<string> { "@method", "@target-uri" }, components);
    }

    [Fact]
    public void GetComponents_FullPostInput_ReturnsAllComponentsInOrder()
    {
        var sigInput =
            "sig1=(\"@method\" \"@target-uri\" \"authorization\" \"content-digest\" \"content-length\" \"content-type\");created=1700000000;keyid=\"test-key\";alg=\"ed25519\"";

        var components = _parser.GetComponents(sigInput);

        Assert.Equal(
            new List<string>
            {
                "@method",
                "@target-uri",
                "authorization",
                "content-digest",
                "content-length",
                "content-type",
            },
            components
        );
    }
}

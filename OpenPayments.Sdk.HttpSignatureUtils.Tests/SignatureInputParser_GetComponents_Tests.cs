using Xunit;

namespace OpenPayments.Sdk.HttpSignatureUtils.Tests;

public class SignatureInputParserTests
{
    [Fact]
    public void GetComponents_WellFormedInput_ReturnsComponentsInOrder()
    {
        var parser = new SignatureInputParser();

        var result = parser.GetComponents(
            "sig1=(\"@method\" \"@target-uri\" \"content-digest\");created=1700000000;"
                + "keyid=\"k\";alg=\"ed25519\""
        );

        Assert.Equal(["@method", "@target-uri", "content-digest"], result);
    }

    [Fact]
    public void GetComponents_SingleComponent_ReturnsIt()
    {
        var parser = new SignatureInputParser();

        var result = parser.GetComponents("sig1=(\"@method\");created=1700000000");

        Assert.Equal(["@method"], result);
    }

    [Fact]
    public void GetComponents_MissingSig1Label_ReturnsNull()
    {
        var parser = new SignatureInputParser();

        // Previously threw IndexOutOfRangeException, so a malformed inbound header crashed the
        // validator instead of failing it.
        Assert.Null(parser.GetComponents("foo=(\"@method\");created=1700000000"));
    }

    [Fact]
    public void GetComponents_EmptyString_ReturnsNull()
    {
        var parser = new SignatureInputParser();

        Assert.Null(parser.GetComponents(""));
    }

    [Fact]
    public void GetComponents_EmptyComponentList_ReturnsNull()
    {
        var parser = new SignatureInputParser();

        Assert.Null(parser.GetComponents("sig1=();created=1700000000"));
    }
}

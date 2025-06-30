using OpenPayments.Sdk.Configuration;
using OpenPayments.Sdk.Extensions;
using Xunit;

namespace OpenPayments.Sdk.Tests.Configuration;

public class OpenPaymentsOptions_Tests
{
    [Fact]
    public void Default_UseUnauthenticatedClient_ShouldBeFalse()
    {
        var options = new OpenPaymentsOptions();

        Assert.False(options.UseUnauthenticatedClient);
    }

    [Fact]
    public void UseUnauthenticatedClient_Extension_ShouldSetFlagToTrue()
    {
        var options = new OpenPaymentsOptions();

        options.UseUnauthenticatedClient();

        Assert.True(options.UseUnauthenticatedClient);
    }
}
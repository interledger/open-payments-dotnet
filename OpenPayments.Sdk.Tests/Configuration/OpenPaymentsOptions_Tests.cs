using Interledger.OpenPayments.Configuration;
using Interledger.OpenPayments.Extensions;
using Xunit;

namespace Interledger.OpenPayments.Tests.Configuration;

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
        var options = new OpenPaymentsOptions() { UseUnauthenticatedClient = true };

        Assert.True(options.UseUnauthenticatedClient);
    }
}

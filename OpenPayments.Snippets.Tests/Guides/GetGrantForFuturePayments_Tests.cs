using AwesomeAssertions;
using OpenPayments.Snippets.Guides;
using OpenPayments.Snippets.Tests.Infrastructure;

namespace OpenPayments.Snippets.Tests.Guides;

public class GetGrantForFuturePayments_Tests : GuideTestBase
{
    [Fact]
    public async Task Run_CompletesInteractiveGrantAndObtainsAccessToken()
    {
        await new GetGrantForFuturePayments(Client).Run();

        Server.IssuedAccessTokens.Should().ContainSingle();
        Server.IncomingPayments.Should().BeEmpty();
        Server.Quotes.Should().BeEmpty();
        Server.OutgoingPayments.Should().BeEmpty();
    }
}

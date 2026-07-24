using AwesomeAssertions;
using OpenPayments.Snippets.Services;

namespace OpenPayments.Snippets.Tests.Services;

public class GnapInteractionHash_Tests
{
    [Fact]
    public void Compute_MatchesGnapSpecExampleVector()
    {
        var hash = GnapInteractionHash.Compute(
            clientNonce: "VJLO6A4CAYLBXHTR0KRO",
            asNonce: "8UPRZ8WDW7OMX42MSB4Z",
            interactRef: "4IFWWIKYBC2PQ6U56NL1",
            grantRequestUri: new Uri("https://server.example.com/tx")
        );

        hash.Should().Be("wH1AF0isGUGcR-IqwVoISQ_39C6qvpQuPkMRtnyODN0");
    }
}

using AwesomeAssertions;
using OpenPayments.Snippets.Guides;
using OpenPayments.Snippets.Tests.Infrastructure;

namespace OpenPayments.Snippets.Tests.Guides;

public class SplitIncomingPayment_Tests : GuideTestBase
{
    [Fact]
    public async Task Run_CreatesTwoLinkedOutgoingPaymentsForMerchantAndPlatform()
    {
        await new SplitIncomingPayment(Client).Run();

        Server.IncomingPayments.Should().HaveCount(2);
        Server.Quotes.Should().HaveCount(2);
        Server.OutgoingPayments.Should().HaveCount(2);

        var merchantIncomingPayment = Server.IncomingPayments.Values.Single(p => p.IncomingAmount!.Value == "9900");
        var platformIncomingPayment = Server.IncomingPayments.Values.Single(p => p.IncomingAmount!.Value == "100");

        var merchantQuote = Server.Quotes.Values.Single(q => q.Receiver == merchantIncomingPayment.Id);
        var platformQuote = Server.Quotes.Values.Single(q => q.Receiver == platformIncomingPayment.Id);

        Server.OutgoingPayments.Values.Should().Contain(p => p.QuoteId == merchantQuote.Id);
        Server.OutgoingPayments.Values.Should().Contain(p => p.QuoteId == platformQuote.Id);
    }
}

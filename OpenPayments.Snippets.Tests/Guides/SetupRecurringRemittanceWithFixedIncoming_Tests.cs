using AwesomeAssertions;
using OpenPayments.Snippets.Guides;
using OpenPayments.Snippets.Tests.Infrastructure;

namespace OpenPayments.Snippets.Tests.Guides;

public class SetupRecurringRemittanceWithFixedIncoming_Tests : GuideTestBase
{
    [Fact]
    public async Task Run_CreatesOutgoingPaymentForFixedIncomingAmount()
    {
        await new SetupRemittanceWithFixedIncoming(Client).Run();

        Server.IncomingPayments.Should().ContainSingle();
        var incomingPayment = Server.IncomingPayments.Values.Single();
        incomingPayment.IncomingAmount!.Value.Should().Be("1500");
        incomingPayment.IncomingAmount.AssetCode.Should().Be("USD");

        Server.Quotes.Should().ContainSingle();
        var quote = Server.Quotes.Values.Single();
        quote.Receiver.Should().Be(incomingPayment.Id);
        quote.DebitAmount.Value.Should().Be("1500");

        Server.OutgoingPayments.Should().ContainSingle();
        var outgoingPayment = Server.OutgoingPayments.Values.Single();
        outgoingPayment.QuoteId.Should().Be(quote.Id);
    }
}

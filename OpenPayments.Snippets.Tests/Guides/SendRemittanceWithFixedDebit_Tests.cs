using AwesomeAssertions;
using OpenPayments.Snippets.Guides;
using OpenPayments.Snippets.Tests.Infrastructure;

namespace OpenPayments.Snippets.Tests.Guides;

public class SendRemittanceWithFixedDebit_Tests : GuideTestBase
{
    [Fact]
    public async Task Run_CreatesOutgoingPaymentWithFixedDebitAmount()
    {
        await new SendRemittanceWithFixedDebit(Client).Run();

        Server.IncomingPayments.Should().ContainSingle();
        var incomingPayment = Server.IncomingPayments.Values.Single();

        Server.Quotes.Should().ContainSingle();
        var quote = Server.Quotes.Values.Single();
        quote.Receiver.Should().Be(incomingPayment.Id);
        quote.DebitAmount.Value.Should().Be("10000");
        quote.DebitAmount.AssetCode.Should().Be("USD");

        Server.OutgoingPayments.Should().ContainSingle();
        var outgoingPayment = Server.OutgoingPayments.Values.Single();
        outgoingPayment.QuoteId.Should().Be(quote.Id);
        outgoingPayment.DebitAmount.Value.Should().Be("10000");
    }
}

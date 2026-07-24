using AwesomeAssertions;
using OpenPayments.Snippets.Guides;
using OpenPayments.Snippets.Tests.Infrastructure;

namespace OpenPayments.Snippets.Tests.Guides;

public class SendRecurringRemittanceWithFixedReceive_Tests : GuideTestBase
{
    [Fact]
    public async Task Run_CreatesOutgoingPaymentWithFixedReceiveAmount()
    {
        await new SendRecurringRemittanceWithFixedReceive(Client).Run();

        Server.IncomingPayments.Should().ContainSingle();
        var incomingPayment = Server.IncomingPayments.Values.Single();

        Server.Quotes.Should().ContainSingle();
        var quote = Server.Quotes.Values.Single();
        quote.Receiver.Should().Be(incomingPayment.Id);
        quote.ReceiveAmount.Value.Should().Be("400000");
        quote.ReceiveAmount.AssetCode.Should().Be("MXN");

        Server.OutgoingPayments.Should().ContainSingle();
        var outgoingPayment = Server.OutgoingPayments.Values.Single();
        outgoingPayment.QuoteId.Should().Be(quote.Id);
    }
}

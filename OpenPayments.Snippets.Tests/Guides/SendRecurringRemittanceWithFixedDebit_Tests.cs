using AwesomeAssertions;
using OpenPayments.Snippets.Guides;
using OpenPayments.Snippets.Tests.Infrastructure;

namespace OpenPayments.Snippets.Tests.Guides;

public class SendRecurringRemittanceWithFixedDebit_Tests : GuideTestBase
{
    [Fact]
    public async Task Run_CreatesOutgoingPaymentDirectlyFromIncomingPayment()
    {
        await new SendRecurringRemittanceWithFixedDebit(Client).Run();

        Server.IncomingPayments.Should().ContainSingle();
        var incomingPayment = Server.IncomingPayments.Values.Single();

        Server.Quotes.Should().BeEmpty();

        Server.OutgoingPayments.Should().ContainSingle();
        var outgoingPayment = Server.OutgoingPayments.Values.Single();
        outgoingPayment.QuoteId.Should().BeNull();
        outgoingPayment.Receiver.Should().Be(incomingPayment.Id);
        outgoingPayment.DebitAmount.Value.Should().Be("20000");
        outgoingPayment.DebitAmount.AssetCode.Should().Be("USD");
    }
}

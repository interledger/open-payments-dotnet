using OpenPayments.Sdk.Clients;

namespace OpenPayments.Snippets.Services.Unauthenticated;

public class IncomingPaymentService(IUnauthenticatedClient client)
{
    private readonly IUnauthenticatedClient _client = client;

    public async Task DisplayIncomingPaymentInfoAsync(string incomingPaymentUrl)
    {
        var incomingPayment = await client.GetIncomingPaymentAsync(incomingPaymentUrl);
        Console.WriteLine("===Incoming Payment Info===");
        Console.WriteLine("AssetCode: {0}", incomingPayment.ReceivedAmount.AssetCode);
        Console.WriteLine("AssetScale: {0}", incomingPayment.ReceivedAmount.AssetScale);
        Console.WriteLine("Value: {0}", incomingPayment.ReceivedAmount.Value);
    }
}
using Newtonsoft.Json;
using OpenPayments.Sdk.Clients;
using OpenPayments.Sdk.Generated.Auth;
using OpenPayments.Sdk.Generated.Resource;

namespace OpenPayments.Snippets.Services.Authenticated;

public class QuoteService(IAuthenticatedClient client)
{
    public async Task<QuoteResponse> CreateQuoteAsync(
        string senderWalletAddress,
        string incomingPaymentUrl
    )
    {
        var waDetails = await client.GetWalletAddressAsync(senderWalletAddress);

        var authResponse = await client.RequestGrantAsync(
            new RequestArgs() { Url = waDetails.AuthServer },
            new GrantCreateBody()
            {
                AccessToken = new AccessToken()
                {
                    Access =
                    [
                        new AccessItem()
                        {
                            Type = AccessType.Quote,
                            Actions = [Actions.Create, Actions.Read],
                        },
                    ],
                },
            }
        );

        var quote = await client.CreateQuoteAsync(
            new AuthRequestArgs()
            {
                Url = waDetails.ResourceServer,
                AccessToken = authResponse.AccessToken!.Value,
            },
            new QuoteBody()
            {
                WalletAddress = waDetails.Id,
                Receiver = new Uri(incomingPaymentUrl),
                Method = PaymentMethod.Ilp,
            }
        );
        Console.WriteLine("===Quote===");
        Console.WriteLine("Id: {0}", quote.Id);
        Console.WriteLine("IncomingPaymentUrl: {0}", quote.Receiver);
        Console.WriteLine("Receive Amount: {0}", quote.ReceiveAmount.Value);
        Console.WriteLine("Debit Amount: {0}", quote.DebitAmount.Value);

        return quote;
    }

    public async Task<QuoteResponse> GetQuoteAsync(string senderWalletAddress, string quoteUrl)
    {
        var waDetails = await client.GetWalletAddressAsync(senderWalletAddress);

        var authResponse = await client.RequestGrantAsync(
            new RequestArgs()
            {
                Url = waDetails.AuthServer
            },
            new GrantCreateBody()
            {
                AccessToken = new AccessToken()
                {
                    Access =
                    [
                        new AccessItem()
                        {
                            Type = AccessType.Quote,
                            Actions = [Actions.Create, Actions.Read]
                        }
                    ]
                }
            }
        );

        var quote = await client.GetQuoteAsync(
            new AuthRequestArgs()
            {
                Url = new Uri(quoteUrl),
                AccessToken = authResponse.AccessToken!.Value,
            }
        );

        Console.WriteLine("===Quote===");
        Console.WriteLine("Id: {0}", quote.Id);
        Console.WriteLine("IncomingPaymentUrl: {0}", quote.Receiver);
        Console.WriteLine("Receive Amount: {0}", quote.ReceiveAmount.Value);
        Console.WriteLine("Debit Amount: {0}", quote.DebitAmount.Value);

        return quote;
    }
}

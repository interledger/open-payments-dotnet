using Newtonsoft.Json;
using OpenPayments.Sdk.Clients;
using OpenPayments.Sdk.Generated.Auth;
using OpenPayments.Sdk.Generated.Resource;
using Amount = OpenPayments.Sdk.Generated.Resource.Amount;

namespace OpenPayments.Snippets.Services.Authenticated;

public class QuoteService(IAuthenticatedClient client)
{
    public async Task CreateQuoteAsync(string senderWalletAddress, string incomingPaymentUrl,
        string? debitAmount, string? receiveAmount)
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
                        new QuoteAccess()
                        {
                            Type = AccessType.Quote,
                            Actions = [Actions.Create, Actions.Read]
                        }
                    ]
                }
            }
        );

        var receiver = new Uri(incomingPaymentUrl);
        var body = (debitAmount, receiveAmount) switch
        {
            (null, null) => QuoteBase(new QuoteBody()),
            (not null, null) => QuoteBase(new QuoteBodyWithDebitAmount
            {
                DebitAmount = new Amount(debitAmount, "EUR")
            }),
            (null, not null) => QuoteBase(new QuoteBodyWithReceiveAmount
            {
                ReceiveAmount = new Amount(receiveAmount, "EUR")
            }),
            _ => throw new Exception("Invalid arguments. Use either debitAmount or receiveAmount or none of them.")
        };

        var quote = await client.CreateQuoteAsync(
            new AuthRequestArgs()
            {
                Url = waDetails.ResourceServer,
                AccessToken = authResponse.AccessToken!.Value,
            },
            body
        );

        Console.WriteLine("===Quote===");
        Console.WriteLine("Id: {0}", quote.Id);
        Console.WriteLine("IncomingPaymentUrl: {0}", quote.Receiver);
        Console.WriteLine("Receive Amount: {0}", quote.ReceiveAmount.Value);
        Console.WriteLine("Debit Amount: {0}", quote.DebitAmount.Value);

        QuoteBody QuoteBase(QuoteBody b)
        {
            b.WalletAddress = waDetails.Id;
            b.Receiver = receiver;
            b.Method = PaymentMethod.Ilp;
            return b;
        }
    }

    public async Task GetQuoteAsync(string senderWalletAddress, string quoteUrl)
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
                        new QuoteAccess()
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
    }
}

using Newtonsoft.Json;
using OpenPayments.Sdk.Clients;
using OpenPayments.Sdk.Generated.Auth;
using OpenPayments.Sdk.Generated.Resource;
using Amount = OpenPayments.Sdk.Generated.Auth.Amount;

namespace OpenPayments.Snippets.Services.Authenticated;

public class OutgoingPaymentService(IAuthenticatedClient client)
{
    public async Task<OutgoingPaymentResponse> CreateOutgoingPaymentAsync(
        string senderWalletAddress,
        string quoteUrl,
        string debitAmount
    )
    {
        var waDetails = await client.GetWalletAddressAsync(senderWalletAddress);

        var grantResponse = await client.RequestGrantAsync(
            new RequestArgs() { Url = waDetails.AuthServer },
            new GrantCreateBody()
            {
                AccessToken = new AccessToken()
                {
                    Access =
                    [
                        new AccessItem()
                        {
                            Type = AccessType.OutgoingPayment,
                            Actions = [Actions.Create, Actions.Read, Actions.List],
                            Identifier = waDetails.Id.ToString(),
                            Limits = new AccessLimits()
                            {
                                DebitAmount = new Amount(debitAmount, "EUR", 2),
                            },
                        },
                    ],
                },
                Interact = new InteractRequest() { Start = [Start.Redirect] },
            }
        );

        Console.WriteLine("Visit the link below, then press enter to continue:");
        Console.WriteLine($"{grantResponse.Interact!.Redirect}");
        Console.ReadLine();

        var tokenResponse = await client.ContinueGrantAsync(
            new AuthRequestArgs()
            {
                Url = grantResponse.Continue.Uri,
                AccessToken = grantResponse.Continue.AccessToken.Value,
            }
        );

        // Create Outgoing Payment
        var outgoing = await client.CreateOutgoingPaymentAsync(
            new AuthRequestArgs()
            {
                Url = waDetails.ResourceServer,
                AccessToken = tokenResponse.AccessToken!.Value,
            },
            new OutgoingPaymentBody() { WalletAddress = waDetails.Id, QuoteId = new Uri(quoteUrl) }
        );

        Console.WriteLine("===Outgoing Payment===");
        Console.WriteLine("Id: {0}", outgoing.Id);
        Console.WriteLine("Quote: {0}", outgoing.QuoteId);
        Console.WriteLine("IncomingPaymentUrl: {0}", outgoing.Receiver);
        Console.WriteLine("Receive Amount: {0}", outgoing.ReceiveAmount.Value);
        Console.WriteLine("Debit Amount: {0}", outgoing.DebitAmount.Value);

        return outgoing;
    }

    public async Task CreateOutgoingPaymentGrantAndCancelAsync(string senderWalletAddress)
    {
        var waDetails = await client.GetWalletAddressAsync(senderWalletAddress);

        var response = await client.RequestGrantAsync(
            new RequestArgs() { Url = waDetails.AuthServer },
            new GrantCreateBody()
            {
                AccessToken = new AccessToken()
                {
                    Access =
                    [
                        new AccessItem()
                        {
                            Type = AccessType.OutgoingPayment,
                            Actions = [Actions.Create, Actions.Read, Actions.List],
                            Identifier = waDetails.Id.ToString(),
                            Limits = new AccessLimits()
                            {
                                DebitAmount = new Amount("100", "EUR", 2),
                            },
                        },
                    ],
                },
                Interact = new InteractRequest() { Start = [Start.Redirect] },
            }
        );

        Console.WriteLine(JsonConvert.SerializeObject(response, Formatting.Indented));
        Console.ReadLine();

        await client.CancelGrantAsync(
            new AuthRequestArgs()
            {
                Url = response.Continue.Uri,
                AccessToken = response.Continue.AccessToken.Value,
            }
        );
    }
}

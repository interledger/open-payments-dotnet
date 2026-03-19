using Newtonsoft.Json;
using OpenPayments.Sdk.Clients;
using OpenPayments.Sdk.Generated.Auth;
using OpenPayments.Sdk.Generated.Resource;
using Amount = OpenPayments.Sdk.Generated.Auth.Amount;

namespace OpenPayments.Snippets.Services.Authenticated;

public class OutgoingPaymentService(IAuthenticatedClient client)
{
    public async Task CreateOutgoingPaymentAsync(
        string senderWalletAddress,
        string debitAmount,
        string? quoteUrl,
        string? incomingPaymentUrl
    )
    {
        var waDetails = await client.GetWalletAddressAsync(senderWalletAddress);

        var grantResponse = await client.RequestGrantAsync(
            new RequestArgs { Url = waDetails.AuthServer },
            new GrantCreateBodyWithInteract
            {
                AccessToken = new AccessToken
                {
                    Access =
                    [
                        new OutgoingAccess
                        {
                            Actions = [Actions.Create, Actions.Read, Actions.List],
                            Identifier = waDetails.Id,
                            Limits = new OutgoingAccessLimits
                            {
                                DebitAmount = new Amount(debitAmount, "EUR", 2),
                            },
                        },
                    ],
                },
                Interact = new InteractRequest
                {
                    Start = [Start.Redirect],
                    // Finish = new Finish()
                    // {
                    //     Method = FinishMethod.Redirect,
                    //     Uri = new Uri("https://localhost"),
                    //     Nonce = Guid.NewGuid().ToString(),
                    // }
                },
            }
        );

        if (grantResponse.Interact == null)
            throw new Exception("No redirect url returned");

        Console.WriteLine("Visit the link below, then press enter to continue:");
        Console.WriteLine($"{grantResponse.Interact.Redirect}");
        Console.ReadLine();

        var tokenResponse = await client.ContinueGrantAsync(
            new AuthRequestArgs()
            {
                Url = grantResponse.Continue.Uri,
                AccessToken = grantResponse.Continue.AccessToken.Value,
            }
        );

        OutgoingPaymentBody body;

        if (incomingPaymentUrl != null)
            body = new OutgoingPaymentBodyFromIncomingPayment()
            {
                WalletAddress = waDetails.Id,
                IncomingPayment = new Uri(incomingPaymentUrl),
                DebitAmount = new Sdk.Generated.Resource.Amount(debitAmount, "EUR"),
            };
        else if (quoteUrl != null)
            body = new OutgoingPaymentBodyFromQuote()
            {
                WalletAddress = waDetails.Id,
                QuoteId = new Uri(quoteUrl),
            };
        else
            throw new Exception("No quote or incoming payment url provided");

        var args = new AuthRequestArgs()
        {
            Url = waDetails.ResourceServer,
            AccessToken = tokenResponse.AccessToken!.Value,
        };

        // Create Outgoing Payment
        var outgoing = body switch
        {
            OutgoingPaymentBodyFromIncomingPayment b => await client.CreateOutgoingPaymentAsync(
                args,
                b
            ),
            OutgoingPaymentBodyFromQuote b => await client.CreateOutgoingPaymentAsync(args, b),
            _ => throw new Exception("Invalid body type"),
        };

        Console.WriteLine("===Outgoing Payment===");
        Console.WriteLine("Id: {0}", outgoing.Id);
        Console.WriteLine("Quote: {0}", outgoing.QuoteId);
        Console.WriteLine("IncomingPaymentUrl: {0}", outgoing.Receiver);
        Console.WriteLine("Receive Amount: {0}", outgoing.ReceiveAmount.Value);
        Console.WriteLine("Debit Amount: {0}", outgoing.DebitAmount.Value);
    }

    public async Task GetOutgoingPaymentAsync(string senderWalletAddress, string outgoingPaymentUrl)
    {
        var waDetails = await client.GetWalletAddressAsync(senderWalletAddress);
        var grantResponse = await client.RequestGrantAsync(
            new RequestArgs() { Url = waDetails.AuthServer },
            new GrantCreateBodyWithInteract()
            {
                AccessToken = new AccessToken()
                {
                    Access =
                    [
                        new OutgoingAccess()
                        {
                            // Type = AccessType.OutgoingPayment,
                            Actions = [Actions.Read, Actions.List],
                            Identifier = waDetails.Id,
                        },
                    ],
                },
                Interact = new InteractRequest() { Start = [Start.Redirect] },
            }
        );

        if (grantResponse.Interact == null)
            throw new Exception("No redirect url returned");

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

        var outgoing = await client.GetOutgoingPaymentAsync(
            new AuthRequestArgs()
            {
                Url = new Uri(outgoingPaymentUrl),
                AccessToken = tokenResponse.AccessToken!.Value,
            }
        );

        Console.WriteLine("===Outgoing Payment===");
        Console.WriteLine("Id: {0}", outgoing.Id);
        Console.WriteLine("Quote: {0}", outgoing.QuoteId);
        Console.WriteLine("IncomingPaymentUrl: {0}", outgoing.Receiver);
        Console.WriteLine("Receive Amount: {0}", outgoing.ReceiveAmount.Value);
        Console.WriteLine("Debit Amount: {0}", outgoing.DebitAmount.Value);
    }

    public async Task ListOutgoingPaymentAsync(string senderWalletAddress)
    {
        var waDetails = await client.GetWalletAddressAsync(senderWalletAddress);

        var grantResponse = await client.RequestGrantAsync(
            new RequestArgs() { Url = waDetails.AuthServer },
            new GrantCreateBodyWithInteract()
            {
                AccessToken = new AccessToken()
                {
                    Access =
                    [
                        new OutgoingAccess()
                        {
                            Type = AccessType.OutgoingPayment,
                            Actions = [Actions.Read, Actions.List],
                            Identifier = waDetails.Id,
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

        var list = await client.ListOutgoingPaymentsAsync(
            new AuthRequestArgs()
            {
                Url = waDetails.ResourceServer,
                AccessToken = tokenResponse.AccessToken!.Value,
            },
            new ListOutgoingPaymentQuery() { WalletAddress = waDetails.Id.ToString() }
        );

        foreach (var oPayment in list.Result!)
        {
            Console.WriteLine("===Incoming Payment===");
            Console.WriteLine("Id: {0}", oPayment.Id);
            Console.WriteLine("Quote: {0}", oPayment.QuoteId);
            Console.WriteLine("IncomingPaymentUrl: {0}", oPayment.Receiver);
            Console.WriteLine("Receive Amount: {0}", oPayment.ReceiveAmount.Value);
            Console.WriteLine("Debit Amount: {0}", oPayment.DebitAmount.Value);
            Console.WriteLine("Sent Amount: {0}", oPayment.SentAmount.Value);
        }
    }

    public async Task CreateOutgoingPaymentGrantAndCancelAsync(string senderWalletAddress)
    {
        var waDetails = await client.GetWalletAddressAsync(senderWalletAddress);

        var response = await client.RequestGrantAsync(
            new RequestArgs() { Url = waDetails.AuthServer },
            new GrantCreateBodyWithInteract()
            {
                AccessToken = new AccessToken()
                {
                    Access =
                    [
                        new OutgoingAccess()
                        {
                            Type = AccessType.OutgoingPayment,
                            Actions = [Actions.Create, Actions.Read, Actions.List],
                            Identifier = waDetails.Id,
                            Limits = new OutgoingAccessLimits()
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

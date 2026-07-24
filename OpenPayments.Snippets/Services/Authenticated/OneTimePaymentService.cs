using System.Diagnostics;
using Interledger.OpenPayments.Clients;
using Interledger.OpenPayments.Generated.Auth;
using Interledger.OpenPayments.Generated.Resource;
using OpenPayments.Snippets.Services;
using ResourceAmount = Interledger.OpenPayments.Generated.Resource.Amount;
using AuthAmount = Interledger.OpenPayments.Generated.Auth.Amount;

namespace OpenPayments.Snippets.Services.Authenticated;

public class OneTimePaymentService(IAuthenticatedClient client)
{
    private static readonly TimeSpan CallbackTimeout = TimeSpan.FromMinutes(5);

    public async Task RunAsync(
        string senderWalletAddress,
        string receiverWalletAddress,
        string amount,
        int callbackPort
    )
    {
        // 1. Resolve wallet addresses
        var senderWaDetails = await client.GetWalletAddressAsync(senderWalletAddress);
        var receiverWaDetails = await client.GetWalletAddressAsync(receiverWalletAddress);

        // 2. Non-interactive incoming-payment grant + create incoming payment (receiver's asset)
        var incomingPaymentGrant = await client.RequestGrantAsync(
            new RequestArgs { Url = receiverWaDetails.AuthServer },
            new GrantCreateBody
            {
                AccessToken = new AccessToken
                {
                    Access = [new IncomingAccess { Actions = [Actions.Create] }],
                },
            }
        );

        if (incomingPaymentGrant.AccessToken == null)
            throw new Exception("Expected a non-interactive incoming payment grant");

        var incomingPayment = await client.CreateIncomingPaymentAsync(
            new AuthRequestArgs
            {
                Url = receiverWaDetails.ResourceServer,
                AccessToken = incomingPaymentGrant.AccessToken.Value,
            },
            new IncomingPaymentBody
            {
                WalletAddress = receiverWaDetails.Id,
                IncomingAmount = new ResourceAmount
                {
                    AssetCode = receiverWaDetails.AssetCode,
                    AssetScale = receiverWaDetails.AssetScale,
                    Value = amount,
                },
            }
        );

        Console.WriteLine("===Incoming Payment===");
        Console.WriteLine("Id: {0}", incomingPayment.Id);
        Console.WriteLine("Amount: {0}", incomingPayment.ReceivedAmount.Value);
        Console.WriteLine("ExpiresAt: {0}", incomingPayment.ExpiresAt);

        // 3. Non-interactive quote grant + create quote for that incoming payment
        var quoteGrant = await client.RequestGrantAsync(
            new RequestArgs { Url = senderWaDetails.AuthServer },
            new GrantCreateBody
            {
                AccessToken = new AccessToken
                {
                    Access = [new QuoteAccess { Actions = [Actions.Create] }],
                },
            }
        );

        if (quoteGrant.AccessToken == null)
            throw new Exception("Expected a non-interactive quote grant");

        var quote = await client.CreateQuoteAsync(
            new AuthRequestArgs
            {
                Url = senderWaDetails.ResourceServer,
                AccessToken = quoteGrant.AccessToken.Value,
            },
            new QuoteBody
            {
                WalletAddress = senderWaDetails.Id,
                Receiver = incomingPayment.Id,
                Method = PaymentMethod.Ilp,
            }
        );

        Console.WriteLine("===Quote===");
        Console.WriteLine("Id: {0}", quote.Id);
        Console.WriteLine("Receive Amount: {0}", quote.ReceiveAmount.Value);
        Console.WriteLine("Debit Amount: {0}", quote.DebitAmount.Value);

        // 4. Start the local callback listener
        using var interactionListener = new GrantInteractionListener();
        await interactionListener.StartAsync(callbackPort);

        var clientNonce = Guid.NewGuid().ToString();
        var callbackUri = new Uri($"http://localhost:{callbackPort}/callback");

        // 5. Interactive outgoing-payment grant, limited to the quote's debit amount
        var outgoingPaymentGrant = await client.RequestGrantAsync(
            new RequestArgs { Url = senderWaDetails.AuthServer },
            new GrantCreateBodyWithInteract
            {
                AccessToken = new AccessToken
                {
                    Access =
                    [
                        new OutgoingAccess
                        {
                            Identifier = senderWaDetails.Id,
                            Actions = [Actions.Create],
                            Limits = new OutgoingAccessLimits
                            {
                                DebitAmount = new AuthAmount(
                                    quote.DebitAmount.Value,
                                    quote.DebitAmount.AssetCode,
                                    quote.DebitAmount.AssetScale
                                ),
                            },
                        },
                    ],
                },
                Interact = new InteractRequest
                {
                    Start = [Start.Redirect],
                    Finish = new Finish
                    {
                        Method = FinishMethod.Redirect,
                        Uri = callbackUri,
                        Nonce = clientNonce,
                    },
                },
            }
        );

        if (outgoingPaymentGrant.Interact == null)
            throw new Exception("Expected an interactive outgoing payment grant");

        var redirectUrl = outgoingPaymentGrant.Interact.Redirect;
        var asFinishNonce = outgoingPaymentGrant.Interact.Finish;

        // 6. Print/open the interactive redirect
        Console.WriteLine("===Interaction Required===");
        Console.WriteLine("Visit the link below to authorize the payment:");
        Console.WriteLine(redirectUrl);

        try
        {
            Process.Start(new ProcessStartInfo(redirectUrl.ToString()) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                "Could not open the browser automatically ({0}); open the URL above manually.",
                ex.Message
            );
        }

        // 7. Wait for the callback
        var callback = await interactionListener.WaitForCallbackAsync(CallbackTimeout);
        var interactRef =
            callback.InteractRef ?? throw new Exception("Callback did not include an interact_ref");

        // 8. Verify the GNAP interaction hash (log-only on mismatch)
        var expectedHash = GnapInteractionHash.Compute(
            clientNonce,
            asFinishNonce,
            interactRef,
            senderWaDetails.AuthServer
        );

        if (callback.Hash != expectedHash)
        {
            Console.WriteLine(
                "WARNING: interaction hash mismatch (got '{0}', expected '{1}'); continuing anyway.",
                callback.Hash,
                expectedHash
            );
        }

        // 9. Continue the grant and create the outgoing payment
        var outgoingPaymentToken = await client.ContinueGrantAsync(
            new AuthRequestArgs
            {
                Url = outgoingPaymentGrant.Continue.Uri,
                AccessToken = outgoingPaymentGrant.Continue.AccessToken.Value,
            },
            new GrantContinueBody { InteractRef = interactRef }
        );

        if (outgoingPaymentToken.AccessToken == null)
            throw new Exception("Expected a non-interactive grant after continuation");

        var outgoingPayment = await client.CreateOutgoingPaymentAsync(
            new AuthRequestArgs
            {
                Url = senderWaDetails.ResourceServer,
                AccessToken = outgoingPaymentToken.AccessToken.Value,
            },
            new OutgoingPaymentBodyFromQuote { WalletAddress = senderWaDetails.Id, QuoteId = quote.Id }
        );

        // 10. Summary
        Console.WriteLine("===Outgoing Payment===");
        Console.WriteLine("Id: {0}", outgoingPayment.Id);
        Console.WriteLine("Quote: {0}", outgoingPayment.QuoteId);
        Console.WriteLine("IncomingPaymentUrl: {0}", outgoingPayment.Receiver);
        Console.WriteLine("Receive Amount: {0}", outgoingPayment.ReceiveAmount.Value);
        Console.WriteLine("Debit Amount: {0}", outgoingPayment.DebitAmount.Value);
    }
}

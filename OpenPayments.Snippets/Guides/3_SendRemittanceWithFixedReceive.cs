using OpenPayments.Sdk.Clients;
using OpenPayments.Sdk.Generated.Auth;
using OpenPayments.Sdk.Generated.Resource;
using AuthAmount = OpenPayments.Sdk.Generated.Auth.Amount;
using Amount = OpenPayments.Sdk.Generated.Resource.Amount;

namespace OpenPayments.Snippets.Guides;

public class SendRemittanceWithFixedReceive(IAuthenticatedClient client) {
    public async Task Run()
    {
        // 1. Get wallet addresses information
        var senderWalletAddress = await client.GetWalletAddressAsync("https://cloudninebank.example.com/sender");
        var recipientWalletAddress = await client.GetWalletAddressAsync("https://happylifebank.example.com/recipient");

        // 2. Request an incoming payment grant
        var recipientIncomingPaymentGrant = await client.RequestGrantAsync(
            new RequestArgs
            {
                Url = recipientWalletAddress.AuthServer,
            },
            new GrantCreateBody
            {
                AccessToken = new AccessToken
                {
                    Access =
                    [
                        new IncomingAccess
                        {
                            Actions = [Actions.Create]
                        }
                    ]
                }
            }
        );

        if (recipientIncomingPaymentGrant.AccessToken == null) throw new Exception("Expected a non-interactive grant");

        // 3. Request the creation of an incoming payment resource
        var recipientIncomingPayment = await client.CreateIncomingPaymentAsync(
            new AuthRequestArgs
            {
                Url = recipientWalletAddress.ResourceServer,
                AccessToken = recipientIncomingPaymentGrant.AccessToken.Value
            },
            new IncomingPaymentBody
            {
                WalletAddress = recipientWalletAddress.Id,
            }
        );

        // 4. Request a quote grant
        var senderQuoteGrant = await client.RequestGrantAsync(
            new RequestArgs
            {
                Url = senderWalletAddress.AuthServer,
            },
            new GrantCreateBody
            {
                AccessToken = new AccessToken
                {
                    Access =
                    [
                        new QuoteAccess
                        {
                            Actions = [Actions.Create]
                        }
                    ]
                }
            }
        );

        if (senderQuoteGrant.AccessToken == null) throw new Exception("Expected a non-interactive grant");

        // 5. Request the creation of a quote resource
        var senderQuote = await client.CreateQuoteAsync(
            new AuthRequestArgs
            {
                Url = senderWalletAddress.ResourceServer,
                AccessToken = senderQuoteGrant.AccessToken.Value
            },
            new QuoteBodyWithReceiveAmount
            {
                Method = PaymentMethod.Ilp,
                WalletAddress = senderWalletAddress.Id,
                Receiver = recipientIncomingPayment.Id,
                ReceiveAmount = new Amount("500000", "MXN", 2)
            }
        );

        var NONCE = Guid.NewGuid().ToString();

        // 6. Request an interactive outgoing payment grant
        var pendingSenderOutgoingPaymentGrant = await client.RequestGrantAsync(
            new RequestArgs
            {
                Url = senderWalletAddress.AuthServer,
            },
            new GrantCreateBodyWithInteract
            {
                AccessToken = new AccessToken
                {
                    Access =
                    [
                        new OutgoingAccess
                        {
                            Identifier = senderWalletAddress.Id,
                            Actions = [Actions.Create],
                            Limits = new OutgoingAccessLimits
                            {
                                ReceiveAmount = new AuthAmount("500000", "MXN", 2)
                            }
                        }
                    ]
                },
                Interact = new InteractRequest
                {
                    Start = [Start.Redirect],
                    Finish = new Finish
                    {
                        Method = FinishMethod.Redirect,
                        Uri = new Uri("https://localhost"),  // where to redirect your user after they've completed the interaction
                        Nonce = NONCE
                    }
                }
            }
        );

        // 7. Start interaction with the customer
        //

        // 8. Finish interaction with the customer
        var interactRef = Guid.NewGuid().ToString();

        // 9. Request a grant continuation
        var senderOutgoingPaymentGrant = await client.ContinueGrantAsync(
            new AuthRequestArgs
            {
                Url = pendingSenderOutgoingPaymentGrant.Continue.Uri,
                AccessToken = pendingSenderOutgoingPaymentGrant.Continue.AccessToken.Value
            },
            new GrantContinueBody
            {
                InteractRef = interactRef
            }
        );

        if (senderOutgoingPaymentGrant.AccessToken == null) throw new Exception("Expected a non-interactive grant");

        // 10. Request the creation of an outgoing payment resource
        var senderOutgoingPayment = await client.CreateOutgoingPaymentAsync(
            new AuthRequestArgs
            {
                Url = senderWalletAddress.ResourceServer,
                AccessToken = senderOutgoingPaymentGrant.AccessToken.Value
            },
            new OutgoingPaymentBodyFromQuote
            {
                WalletAddress = senderWalletAddress.Id,
                QuoteId = senderQuote.Id,
            }
        );
    }
}

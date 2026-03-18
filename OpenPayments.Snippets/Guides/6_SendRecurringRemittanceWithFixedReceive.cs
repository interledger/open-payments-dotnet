using OpenPayments.Sdk.Clients;
using OpenPayments.Sdk.Generated.Auth;
using OpenPayments.Sdk.Generated.Resource;
using AuthAmount = OpenPayments.Sdk.Generated.Auth.Amount;
using Amount = OpenPayments.Sdk.Generated.Resource.Amount;

namespace OpenPayments.Snippets.Guides;

public class SendRecurringRemittanceWithFixedReceive(IAuthenticatedClient client)
{
    public async Task Run()
    {
        // 1. Get wallet addresses information
        var senderWalletAddress = await client.GetWalletAddressAsync("https://cloudninebank.example.com/sender");
        var recipientWalletAddress =
            await client.GetWalletAddressAsync("https://happylifebank.example.com/recipient");

        var NONCE = Guid.NewGuid().ToString();

        // 2. Request an interactive outgoing payment grant
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
                                DebitAmount = new AuthAmount("400000", "MXN", 2),
                                Interval = "R3/2025-10-03T23:25:00Z/P1M"
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
                        Uri = new Uri(
                            "https://localhost"), // where to redirect your user after they've completed the interaction
                        Nonce = NONCE
                    }
                }
            }
        );

        // 3. Start interaction with the sender
        //

        // 4. Finish interaction with the sender
        var interactRef = Guid.NewGuid().ToString();

        // 5. Request a grant continuation
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

        // 6. Request an incoming payment grant
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
                            Actions = [Actions.Create],
                        }
                    ]
                }
            }
        );

        if (recipientIncomingPaymentGrant.AccessToken == null) throw new Exception("Expected a non-interactive grant");

        // 7. Request the creation of an incoming payment resource
        var recipientIncomingPayment = await client.CreateIncomingPaymentAsync(
            new AuthRequestArgs
            {
                Url = recipientWalletAddress.ResourceServer,
                AccessToken = recipientIncomingPaymentGrant.AccessToken.Value
            },
            new IncomingPaymentBody
            {
                WalletAddress = recipientWalletAddress.Id
            }
        );

        // 8. Request a quote grant
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
                            Actions = [Actions.Create],
                        }
                    ]
                }
            }
        );

        if (senderQuoteGrant.AccessToken == null) throw new Exception("Expected a non-interactive grant");

        // 9. Request the creation of a quote resource
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
                ReceiveAmount = new Amount("400000", "MXN", 2)
            }
        );

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

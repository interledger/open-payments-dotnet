using OpenPayments.Sdk.Clients;
using OpenPayments.Sdk.Generated.Auth;
using OpenPayments.Sdk.Generated.Resource;
using AuthAmount = OpenPayments.Sdk.Generated.Auth.Amount;
using Amount = OpenPayments.Sdk.Generated.Resource.Amount;

namespace OpenPayments.Snippets.Guides;

public class SendRecurringRemittanceWithFixedDebit(IAuthenticatedClient client)
{
    public async Task Run()
    {
        // 1. Get wallet addresses information
        var senderWalletAddress = await client.GetWalletAddressAsync("https://cloudninebank.example.com/sender");
        var recipientWalletAddress =
            await client.GetWalletAddressAsync("https://happylifebank.example.com/recipient");

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
                WalletAddress = recipientWalletAddress.Id
            }
        );

        var NONCE = Guid.NewGuid().ToString();

        // 4. Request an interactive outgoing payment grant
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
                                DebitAmount = new AuthAmount("20000", "USD", 2),
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

        // 5. Start interaction with the sender
        //

        // 6. Finish interaction with the sender
        var interactRef = Guid.NewGuid().ToString();

        // 7. Request a grant continuation
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

        // 8. Request the creation of an outgoing payment resource
        var senderOutgoingPayment = await client.CreateOutgoingPaymentAsync(
            new AuthRequestArgs
            {
                Url = senderWalletAddress.ResourceServer,
                AccessToken = senderOutgoingPaymentGrant.AccessToken.Value
            },
            new OutgoingPaymentBodyFromIncomingPayment
            {
                WalletAddress = senderWalletAddress.Id,
                IncomingPayment = recipientIncomingPayment.Id,
                DebitAmount = new Amount("20000", "USD", 2)
            }
        );
    }
}

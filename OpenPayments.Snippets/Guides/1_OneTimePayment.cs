using OpenPayments.Sdk.Clients;
using OpenPayments.Sdk.Generated.Auth;
using OpenPayments.Sdk.Generated.Resource;
using Amount = OpenPayments.Sdk.Generated.Resource.Amount;
using AuthAmount = OpenPayments.Sdk.Generated.Auth.Amount;

namespace OpenPayments.Snippets.Guides;

public class OneTimePayment(IAuthenticatedClient client)
{
    public async Task Run()
    {
        // 1. Get wallet addresses information
        var customerWalletAddress = await client.GetWalletAddressAsync("https://cloudninebank.example.com/customer");
        var retailerWalletAddress = await client.GetWalletAddressAsync("https://happylifebank.example.com/retailer");

        // 2. Request an incoming payment grant
        var retailerIncomingPaymentGrant = await client.RequestGrantAsync(
            new RequestArgs
            {
                Url = retailerWalletAddress.AuthServer,
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

        // 3. Request the creation of an incoming payment resource
        var retailerIncomingPayment = await client.CreateIncomingPaymentAsync(
            new AuthRequestArgs
            {
                Url = retailerWalletAddress.ResourceServer,
                AccessToken = retailerIncomingPaymentGrant.AccessToken.Value
            },
            new IncomingPaymentBody
            {
                WalletAddress = retailerWalletAddress.Id,
                IncomingAmount = new Amount("140000", "MXN", 2)
            }
        );

        // 4. Request a quote grant
        var customerQuoteGrant = await client.RequestGrantAsync(
            new RequestArgs
            {
                Url = customerWalletAddress.AuthServer,
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

        if (customerQuoteGrant.AccessToken == null) throw new Exception("Expected a non-interactive grant");

        // 5. Request the creation of a quote resource
        var customerQuote = await client.CreateQuoteAsync(
            new AuthRequestArgs
            {
                Url = customerWalletAddress.ResourceServer,
                AccessToken = customerQuoteGrant.AccessToken.Value
            },
            new QuoteBody
            {
                WalletAddress = customerWalletAddress.Id,
                Receiver = retailerWalletAddress.Id,
                Method = PaymentMethod.Ilp
            }
        );

        var NONCE = Guid.NewGuid().ToString();

        // 6. Request an interactive outgoing payment grant
        var pendingCustomerOutgoingPaymentGrant = await client.RequestGrantAsync(
            new RequestArgs
            {
                Url = customerWalletAddress.AuthServer,
            },
            new GrantCreateBodyWithInteract
            {
                AccessToken = new AccessToken
                {
                    Access =
                    [
                        new OutgoingAccess
                        {
                            Identifier = customerWalletAddress.Id,
                            Actions = [Actions.Create],
                            Limits = new OutgoingAccessLimits
                            {
                                DebitAmount = new AuthAmount("140000", "MXN", 2)
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
                        Uri = new Uri("https://localhost"),
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
        var customerOutgoingPaymentGrant = await client.ContinueGrantAsync(
            new AuthRequestArgs
            {
                Url = pendingCustomerOutgoingPaymentGrant.Continue.Uri,
                AccessToken = pendingCustomerOutgoingPaymentGrant.Continue.AccessToken.Value
            },
            new GrantContinueBody
            {
                InteractRef = interactRef
            }
        );

        if (customerOutgoingPaymentGrant.AccessToken == null) throw new Exception("Expected a non-interactive grant");

        // 10. Request the creation of an outgoing payment resource
        var customerOutgoingPayment = await client.CreateOutgoingPaymentAsync(
            new AuthRequestArgs
            {
                Url = customerWalletAddress.ResourceServer,
                AccessToken = customerOutgoingPaymentGrant.AccessToken.Value
            },
            new OutgoingPaymentBodyFromQuote
            {
                WalletAddress = customerWalletAddress.Id,
                QuoteId = customerQuote.Id,
            }
        );
    }
}

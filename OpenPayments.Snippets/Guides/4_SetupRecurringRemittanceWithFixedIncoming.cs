using OpenPayments.Sdk.Clients;
using OpenPayments.Sdk.Generated.Auth;
using OpenPayments.Sdk.Generated.Resource;
using AuthAmount = OpenPayments.Sdk.Generated.Auth.Amount;
using Amount = OpenPayments.Sdk.Generated.Resource.Amount;

namespace OpenPayments.Snippets.Guides;

public class SetupRemittanceWithFixedIncoming(IAuthenticatedClient client)
{
    public async Task Run()
    {
        // 1. Get wallet addresses information
        var customerWalletAddress = await client.GetWalletAddressAsync("https://cloudninebank.example.com/customer");
        var serviceProviderWalletAddress =
            await client.GetWalletAddressAsync("https://happylifebank.example.com/service-provider");

        // 2. Request an incoming payment grant
        var serviceProviderIncomingPaymentGrant = await client.RequestGrantAsync(
            new RequestArgs
            {
                Url = serviceProviderWalletAddress.AuthServer,
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

        if (serviceProviderIncomingPaymentGrant.AccessToken == null)
            throw new Exception("Expected a non-interactive grant");

        // 3. Request the creation of an incoming payment resource
        var serviceProviderIncomingPayment = await client.CreateIncomingPaymentAsync(
            new AuthRequestArgs
            {
                Url = serviceProviderWalletAddress.ResourceServer,
                AccessToken = serviceProviderIncomingPaymentGrant.AccessToken.Value
            },
            new IncomingPaymentBody
            {
                WalletAddress = serviceProviderWalletAddress.Id,
                IncomingAmount = new Amount("1500", "USD", 2)
            }
        );

        // 4. Request a quote grant
        var customerQuoteGrant = await client.RequestGrantAsync(
            new RequestArgs
            {
                Url = customerWalletAddress.AuthServer
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
                Method = PaymentMethod.Ilp,
                WalletAddress = customerWalletAddress.Id,
                Receiver = serviceProviderIncomingPayment.Id,
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
                            Actions = [Actions.Create, Actions.Read],
                            Limits = new OutgoingAccessLimits
                            {
                                DebitAmount = new AuthAmount("1500", "USD", 2),
                                Interval = "R12/2025-10-14T00:03:00Z/P1M"
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

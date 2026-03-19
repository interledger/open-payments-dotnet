using OpenPayments.Sdk.Clients;
using OpenPayments.Sdk.Generated.Auth;
using OpenPayments.Sdk.Generated.Resource;
using AuthAmount = OpenPayments.Sdk.Generated.Auth.Amount;
using Amount = OpenPayments.Sdk.Generated.Resource.Amount;

namespace OpenPayments.Snippets.Guides;

public class SplitIncomingPayment(IAuthenticatedClient client)
{
    public async Task Run()
    {
        // 1. Get wallet addresses information
        var customerWalletAddress = await client.GetWalletAddressAsync("https://cloudninebank.example.com/customer");
        var merchantWalletAddress =
            await client.GetWalletAddressAsync("https://happylifebank.example.com/merchant");
        var platformWalletAddress =
            await client.GetWalletAddressAsync("https://happylifebank.example.com/platform");

        // 2. Request incoming payment grants
        // Merchant
        var merchantIncomingPaymentGrant = await client.RequestGrantAsync(
            new RequestArgs
            {
                Url = merchantWalletAddress.AuthServer,
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
        // Platform
        var platformIncomingPaymentGrant = await client.RequestGrantAsync(
            new RequestArgs
            {
                Url = platformWalletAddress.AuthServer,
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

        if (merchantIncomingPaymentGrant.AccessToken == null) throw new Exception("Expected a non-interactive grant");
        if (platformIncomingPaymentGrant.AccessToken == null) throw new Exception("Expected a non-interactive grant");

        // 3. Request the creation of incoming payment resources
        // Merchant
        var merchantIncomingPayment = await client.CreateIncomingPaymentAsync(
            new AuthRequestArgs
            {
                Url = merchantWalletAddress.ResourceServer,
                AccessToken = merchantIncomingPaymentGrant.AccessToken.Value
            },
            new IncomingPaymentBody
            {
                WalletAddress = merchantWalletAddress.Id,
                IncomingAmount = new Amount("9900", "USD", 2)
            }
        );
        // Platform
        var platformIncomingPayment = await client.CreateIncomingPaymentAsync(
            new AuthRequestArgs
            {
                Url = platformWalletAddress.ResourceServer,
                AccessToken = platformIncomingPaymentGrant.AccessToken.Value
            },
            new IncomingPaymentBody
            {
                WalletAddress = platformWalletAddress.Id,
                IncomingAmount = new Amount("100", "USD", 2)
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

        // 5. Request the creation of quote resources
        // Merchant
        var merchantQuote = await client.CreateQuoteAsync(
            new AuthRequestArgs
            {
                Url = customerWalletAddress.ResourceServer,
                AccessToken = customerQuoteGrant.AccessToken.Value
            },
            new QuoteBody
            {
                Method = PaymentMethod.Ilp,
                WalletAddress = customerWalletAddress.Id,
                Receiver = merchantIncomingPayment.Id,
            }
        );
        // Platform
        var platformQuote = await client.CreateQuoteAsync(
            new AuthRequestArgs
            {
                Url = customerWalletAddress.ResourceServer,
                AccessToken = customerQuoteGrant.AccessToken.Value
            },
            new QuoteBody
            {
                Method = PaymentMethod.Ilp,
                WalletAddress = customerWalletAddress.Id,
                Receiver = platformIncomingPayment.Id,
            }
        );

        var NONCE = Guid.NewGuid().ToString();

        // 6. Request an interactive outgoing payment grant
        var combinedQuoteAmount = "10000"; // 9900 + 100

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
                                DebitAmount = new AuthAmount(combinedQuoteAmount, "USD", 2),
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

        // 10. Request the creation of outgoing payment resources
        // Merchant
        var customerOutgoingPaymentToMerchant = await client.CreateOutgoingPaymentAsync(
            new AuthRequestArgs
            {
                Url = customerWalletAddress.ResourceServer,
                AccessToken = customerOutgoingPaymentGrant.AccessToken.Value
            },
            new OutgoingPaymentBodyFromQuote
            {
                WalletAddress = customerWalletAddress.Id,
                QuoteId = merchantQuote.Id,
            }
        );
        // Platform
        var customerOutgoingPaymentToPlatform = await client.CreateOutgoingPaymentAsync(
            new AuthRequestArgs
            {
                Url = customerWalletAddress.ResourceServer,
                AccessToken = customerOutgoingPaymentGrant.AccessToken.Value
            },
            new OutgoingPaymentBodyFromQuote
            {
                WalletAddress = customerWalletAddress.Id,
                QuoteId = platformQuote.Id,
            }
        );
    }
}

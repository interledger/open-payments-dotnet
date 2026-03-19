using OpenPayments.Sdk.Clients;
using OpenPayments.Sdk.Generated.Auth;
using OpenPayments.Sdk.Generated.Resource;
using AuthAmount = OpenPayments.Sdk.Generated.Auth.Amount;
using Amount = OpenPayments.Sdk.Generated.Resource.Amount;

namespace OpenPayments.Snippets.Guides;

public class GetGrantForFuturePayments(IAuthenticatedClient client)
{
    public async Task Run()
    {
        // 1. Get wallet address information
        var userWalletAddress = await client.GetWalletAddressAsync("https://cloudninebank.example.com/user");

        var NONCE = Guid.NewGuid().ToString();

        // 2. Request an interactive outgoing payment grant
        var grant = await client.RequestGrantAsync(
            new RequestArgs
            {
                Url = userWalletAddress.AuthServer,
            },
            new GrantCreateBodyWithInteract
            {
                AccessToken = new AccessToken
                {
                    Access =
                    [
                        new OutgoingAccess
                        {
                            Identifier = userWalletAddress.Id,
                            Actions = [Actions.Create, Actions.Read],
                            Limits = new OutgoingAccessLimits
                            {
                                DebitAmount = new AuthAmount("10000", "CAD", 2),
                                Interval = "R3/2025-05-20T13:00:00Z/P1M"
                            }
                        }
                    ]
                },
                Client = userWalletAddress.Id,
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

        var interactRef = Guid.NewGuid().ToString();

        // 5. Request a grant continuation
        var userOutgoingPaymentGrant = await client.ContinueGrantAsync(
            new AuthRequestArgs
            {
                Url = grant.Continue.Uri,
                AccessToken = grant.Continue.AccessToken.Value
            },
            new GrantContinueBody()
            {
                InteractRef = interactRef
            }
        );
    }
}

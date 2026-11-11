using OpenPayments.Sdk.Clients;
using OpenPayments.Sdk.Generated.Auth;

namespace OpenPayments.Snippets.Guides;

public class VerifyWalletAddressOwnership(IAuthenticatedClient client)
{
    public async Task Run()
    {
        // 1. Get wallet address information
        var userWalletAddress = await client.GetWalletAddressAsync("https://cloudninebank.example.com/user");

        var NONCE = Guid.NewGuid().ToString();

        // 2. Request an interactive grant with a subject
        var grant = await client.RequestGrantAsync(
            new RequestArgs
            {
                Url = userWalletAddress.AuthServer,
            },
            new GrantCreateBodyWithInteract
            {
                Subject = new Subject
                {
                    Sub_ids =
                    [
                        new Sub_ids
                        {
                            Id = userWalletAddress.Id.OriginalString,
                            Format = Sub_idsFormat.Uri
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
                            "https://paymentplatform.example/finish"), // where to redirect the user to after they've completed the interaction
                        Nonce = NONCE
                    }
                }
            }
        );

        var interactRef = Guid.NewGuid().ToString();

        // 5. Request a grant continuation
        var userVerificationGrant = await client.ContinueGrantAsync(
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

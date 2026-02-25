using Newtonsoft.Json;
using OpenPayments.Sdk.Clients;
using OpenPayments.Sdk.Generated.Auth;

namespace OpenPayments.Snippets.Services.Authenticated;

public class TokenService(IAuthenticatedClient client)
{
    public async Task CreateAndRotateTokenAsync(string senderWalletAddress)
    {
        var waDetails = await client.GetWalletAddressAsync(senderWalletAddress);

        var grantResponse = await client.RequestGrantAsync(
            new RequestArgs()
            {
                Url = waDetails.AuthServer
            },
            new GrantCreateBody()
            {
                AccessToken = new AccessToken()
                {
                    Access =
                    [
                        new AccessItem()
                        {
                            Type = AccessType.OutgoingPayment,
                            Actions = [Actions.Create, Actions.Read, Actions.List],
                            Identifier = waDetails.Id.ToString(),
                            Limits = new AccessLimits()
                            {
                                DebitAmount = new Amount("10000", "EUR"),
                            }
                        }
                    ],
                    ExpiresIn = 600
                },
                Interact = new InteractRequest()
                {
                    Start = [Start.Redirect]
                }
            }
        );

        // Console.WriteLine(JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine(JsonConvert.SerializeObject(grantResponse, Formatting.Indented));
        Console.ReadLine();

        var tokenResponse = await client.ContinueGrantAsync(
            new AuthRequestArgs()
            {
                Url = grantResponse.Continue.Uri,
                AccessToken = grantResponse.Continue.AccessToken.Value,
            }
        );

        Console.WriteLine(JsonConvert.SerializeObject(tokenResponse, Formatting.Indented));
        Console.ReadLine();

        var rotatedToken = await RotateTokenAsync(tokenResponse.AccessToken!.Manage, tokenResponse.AccessToken.Value);
        Console.ReadLine();

        await RevokeTokenAsync(rotatedToken.AccessToken.Manage, rotatedToken.AccessToken.Value);
    }

    public async Task<RotateTokenResponse> RotateTokenAsync(string tokenUrl, string accessTokenValue)
    {
        var rotatedToken = await client.RotateTokenAsync(new AuthRequestArgs()
        {
            Url = new Uri(tokenUrl),
            AccessToken = accessTokenValue,
        });

        Console.WriteLine(JsonConvert.SerializeObject(rotatedToken, Formatting.Indented));

        return rotatedToken;
    }

    public async Task RevokeTokenAsync(string tokenUrl, string accessTokenValue)
    {
        await client.RevokeTokenAsync(new AuthRequestArgs()
        {
            Url = new Uri(tokenUrl),
            AccessToken = accessTokenValue,
        });
    }
}
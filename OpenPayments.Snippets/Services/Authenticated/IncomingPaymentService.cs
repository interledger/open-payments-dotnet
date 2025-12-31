using Newtonsoft.Json;
using OpenPayments.Sdk.Clients;
using OpenPayments.Sdk.Generated.Auth;
using OpenPayments.Sdk.Generated.Resource;
using Amount = OpenPayments.Sdk.Generated.Resource.Amount;

namespace OpenPayments.Snippets.Services.Authenticated;

public class IncomingPaymentService(IAuthenticatedClient client, IUnauthenticatedClient unauthenticatedClient)
{
    public async Task<IncomingPaymentResponse> CreateIncomingPaymentAsync(string receiver, string amount)
    {
        var waDetails = await unauthenticatedClient.GetWalletAddressAsync(receiver);
        
        var grant = await client.RequestGrantAsync(
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
                            Type = "incoming-payment",
                            Actions = ["create", "read", "list", "complete"]
                        }
                    ]
                }
            }
        );

        var iPaymentResponse = await client.CreateIncomingPaymentAsync(
            new RequestArgs()
            {
                Url = waDetails.ResourceServer,
                AccessToken = grant.AccessToken!.Value
            },
            new IncomingPaymentBody()
            {
                WalletAddress = waDetails.Id,
                IncomingAmount = new Amount()
                {
                    AssetCode = waDetails.AssetCode,
                    AssetScale = waDetails.AssetScale,
                    Value = amount
                }
            }
        );
        
        Console.WriteLine("===Incoming Payment===");
        Console.WriteLine("Id: {0}", iPaymentResponse.Id);
        Console.WriteLine("Amount: {0}", iPaymentResponse.IncomingAmount.Value);
        Console.WriteLine("ExpiresAt: {0}", iPaymentResponse.ExpiresAt);

        return iPaymentResponse;
    }
}
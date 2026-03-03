using Newtonsoft.Json;
using OpenPayments.Sdk.Clients;
using OpenPayments.Sdk.Generated.Auth;
using OpenPayments.Sdk.Generated.Resource;
using Amount = OpenPayments.Sdk.Generated.Resource.Amount;

namespace OpenPayments.Snippets.Services.Authenticated;

public class IncomingPaymentService(IAuthenticatedClient client)
{
    public async Task<IncomingPaymentResponse> CreateIncomingPaymentAsync(
        string receiver,
        string amount
    )
    {
        var waDetails = await client.GetWalletAddressAsync(receiver);
        var grant = await client.RequestGrantAsync(
            new RequestArgs() { Url = waDetails.AuthServer },
            new GrantCreateBody()
            {
                AccessToken = new AccessToken()
                {
                    Access =
                    [
                        new AccessItem()
                        {
                            Type = AccessType.IncomingPayment,
                            Actions =
                            [
                                Actions.Create,
                                Actions.Read,
                                Actions.List,
                                Actions.Complete,
                            ],
                        },
                    ],
                },
            }
        );

        var iPaymentResponse = await client.CreateIncomingPaymentAsync(
            new AuthRequestArgs()
            {
                Url = waDetails.ResourceServer,
                AccessToken = grant.AccessToken!.Value,
            },
            new IncomingPaymentBody()
            {
                WalletAddress = waDetails.Id,
                IncomingAmount = new Amount()
                {
                    AssetCode = waDetails.AssetCode,
                    AssetScale = waDetails.AssetScale,
                    Value = amount,
                },
            }
        );

        Console.WriteLine("===Incoming Payment===");
        Console.WriteLine("grant: {0}", JsonConvert.SerializeObject(grant, Formatting.None));
        Console.WriteLine("AccessToken: {0}", grant.AccessToken!.Value);
        Console.WriteLine("Id: {0}", iPaymentResponse.Id);
        Console.WriteLine("Amount: {0}", iPaymentResponse.IncomingAmount.Value);
        Console.WriteLine("ExpiresAt: {0}", iPaymentResponse.ExpiresAt);

        return iPaymentResponse;
    }

    public async Task<IncomingPaymentResponse> GetIncomingPaymentAsync(
        string incomingPaymentUrl,
        string accessToken,
        string tokenUrl
    )
    {
        var rotatedToken = await client.RotateTokenAsync(
            new AuthRequestArgs() { Url = new Uri(tokenUrl), AccessToken = accessToken }
        );

        var iPaymentResponse = await client.GetIncomingPaymentAsync(
            new AuthRequestArgs()
            {
                Url = new Uri(incomingPaymentUrl),
                AccessToken = rotatedToken.AccessToken.Value,
            }
        );

        Console.WriteLine("===Incoming Payment===");
        Console.WriteLine("Id: {0}", iPaymentResponse.Id);
        Console.WriteLine("Amount: {0}", iPaymentResponse.IncomingAmount.Value);
        Console.WriteLine("ExpiresAt: {0}", iPaymentResponse.ExpiresAt);
        Console.WriteLine("Access Token Manage: {0}", rotatedToken.AccessToken.Manage);
        Console.WriteLine("Access Token Value: {0}", rotatedToken.AccessToken.Value);

        return iPaymentResponse;
    }

    public async Task CompleteIncomingPaymentAsync(
        string incomingPaymentUrl,
        string accessToken,
        string tokenUrl
    )
    {
    }

    public async Task<ListIncomingPaymentsResponse> ListIncomingPaymentsAsync(string walletAddress)
    {
        var waDetails = await client.GetWalletAddressAsync(walletAddress);

        var grant = await client.RequestGrantAsync(
            new RequestArgs() { Url = waDetails.AuthServer },
            new GrantCreateBody()
            {
                AccessToken = new AccessToken()
                {
                    Access =
                    [
                        new AccessItem()
                        {
                            Type = AccessType.IncomingPayment,
                            Actions = [Actions.List],
                        },
                    ],
                },
            }
        );

        var list = await client.ListIncomingPaymentsAsync(
            new AuthRequestArgs()
            {
                Url = waDetails.ResourceServer,
                AccessToken = grant.AccessToken!.Value,
            },
            new ListIncomingPaymentQuery() { WalletAddress = waDetails.Id.ToString() }
        );

        Console.WriteLine(JsonConvert.SerializeObject(list, Formatting.Indented));

        foreach (var iPayment in list.Result)
        {
            Console.WriteLine("===Incoming Payment===");
            Console.WriteLine("Id: {0}", iPayment.Id);
            Console.WriteLine("Completed: {0}", iPayment.Completed ? "Yes" : "No");
            Console.WriteLine(
                "Amount: {0}/{1} {2}",
                iPayment.ReceivedAmount.Value,
                iPayment.IncomingAmount.Value,
                iPayment.ReceivedAmount.AssetCode
            );
            Console.WriteLine("ExpiresAt: {0}", iPayment.ExpiresAt);
        }

        return list;
    }
}

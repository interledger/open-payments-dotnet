using Newtonsoft.Json;
using OpenPayments.Sdk.Clients;
using OpenPayments.Sdk.Generated.Auth;
using OpenPayments.Sdk.Generated.Resource;
using Amount = OpenPayments.Sdk.Generated.Auth.Amount;

namespace OpenPayments.Snippets.Services.Authenticated;

public class OutgoingPaymentService(IAuthenticatedClient client, IUnauthenticatedClient unauthenticatedClient)
{
    public async Task<OutgoingPaymentResponse> CreateOutgoingPaymentAsync(string senderWalletAddress, string quoteUrl, string debitAmount)
    {
        var waDetails = await unauthenticatedClient.GetWalletAddressAsync(senderWalletAddress);

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
                            Type = "outgoing-payment",
                            Actions = ["create", "read", "list"],
                            Identifier = waDetails.Id.ToString(),
                            Limits = new AccessLimits()
                            {
                                DebitAmount = new Amount()
                                {
                                    Value = debitAmount,
                                    AssetCode = "EUR",
                                    AssetScale = 2
                                },
                            }
                        }
                    ]
                },
                Interact = new InteractRequest()
                {
                    Start = [Start.Redirect],
                    // Finish = new Finish()
                    // {
                    //     Method = FinishMethod.Redirect,
                    //     Uri = new Uri("https://localhost"),
                    //     Nonce = Guid.NewGuid().ToString(),
                    // }
                }
            }
        );

        Console.WriteLine("Visit the link below, then press enter to continue:");
        Console.WriteLine($"{grantResponse.Interact!.Redirect}");
        Console.ReadLine();

        var tokenResponse = await client.ContinueGrantAsync(
            new RequestArgs()
            {
                Url = grantResponse.Continue!.Uri,
                AccessToken = grantResponse.Continue.Access_token.Value,
            },
            new GrantContinueBody()
        );

        // Create Outgoing Payment
        var outgoing = await client.CreateOutgoingPaymentAsync(
            new RequestArgs()
            {
                Url = waDetails.ResourceServer,
                AccessToken = tokenResponse.AccessToken!.Value
            },
            new OutgoingPaymentBody()
            {
                WalletAddress = waDetails.Id,
                QuoteId = new Uri(quoteUrl)
            }
        );
        
        Console.WriteLine("===Outgoing Payment===");
        Console.WriteLine("Id: {0}", outgoing.Id);
        Console.WriteLine("Quote: {0}", outgoing.QuoteId);
        Console.WriteLine("IncomingPaymentUrl: {0}", outgoing.Receiver);
        Console.WriteLine("Receive Amount: {0}", outgoing.ReceiveAmount.Value);
        Console.WriteLine("Debit Amount: {0}", outgoing.DebitAmount.Value);

        return outgoing;
    }


    public async Task CreateOutgoingPaymentGrantAndCancelAsync()
    {
        var response = await client.RequestGrantAsync(
            new RequestArgs()
            {
                Url = new Uri("https://auth.interledger-test.dev/f537937b-7016-481b-b655-9f0d1014822c")
            },
            new GrantCreateBody()
            {
                AccessToken = new AccessToken()
                {
                    Access =
                    [
                        new AccessItem()
                        {
                            Type = "outgoing-payment",
                            Actions = ["create", "read", "list"],
                            Identifier = "https://ilp.interledger-test.dev/cozmin-eur",
                            Limits = new AccessLimits()
                            {
                                DebitAmount = new Amount()
                                {
                                    Value = "100",
                                    AssetCode = "EUR",
                                    AssetScale = 2
                                }
                            }
                        }
                    ]
                },
                Interact = new InteractRequest()
                {
                    Start = [Start.Redirect]
                }
            }
        );

        // Console.WriteLine(JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine(JsonConvert.SerializeObject(response, Formatting.Indented));
        Console.ReadLine();

        await client.CancelGrantAsync(
            new RequestArgs()
            {
                Url = response.Continue!.Uri,
                AccessToken = response.Continue.Access_token.Value,
            }
        );
    }

    public async Task ContinueOutgoingPaymentGrantAsync()
    {
        var response = await client.ContinueGrantAsync(
            new RequestArgs()
            {
                Url = new Uri("https://auth.interledger-test.dev/continue/51f225ff-de47-4103-9c57-80ec2c5fa854"),
                AccessToken = "611060C78F3DFE91649F",
            },
            new GrantContinueBody()
            {
                InteractRef = "56248e15-2a8c-4b38-9915-8f2e7ae93613"
            }
        );

        Console.WriteLine(JsonConvert.SerializeObject(response, Formatting.Indented));
    }
}
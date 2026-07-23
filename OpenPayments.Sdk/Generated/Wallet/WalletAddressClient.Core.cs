using Newtonsoft.Json;

namespace Interledger.OpenPayments.Generated.Wallet;

public partial class WalletAddressClient : GeneratedClientBase
{
    private static readonly JsonSerializerSettings SerializerSettings = new();

    public WalletAddressClient(HttpClient httpClient)
        : base(httpClient, SerializerSettings) { }

    partial void PrepareRequest(HttpClient client, HttpRequestMessage request, string url);

    partial void PrepareRequest(
        HttpClient client,
        HttpRequestMessage request,
        System.Text.StringBuilder urlBuilder
    );

    partial void ProcessResponse(HttpClient client, HttpResponseMessage response);
}

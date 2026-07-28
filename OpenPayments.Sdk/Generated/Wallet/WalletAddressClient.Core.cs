using OpenPayments.Sdk.Serialization;

namespace OpenPayments.Sdk.Generated.Wallet;

public partial class WalletAddressClient : GeneratedClientBase
{
    public WalletAddressClient(HttpClient httpClient)
        : base(httpClient, OpenPaymentsSerialization.DefaultSettings) { }

    partial void PrepareRequest(HttpClient client, HttpRequestMessage request, string url);

    partial void PrepareRequest(
        HttpClient client,
        HttpRequestMessage request,
        System.Text.StringBuilder urlBuilder
    );

    partial void ProcessResponse(HttpClient client, HttpResponseMessage response);
}

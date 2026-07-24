using Interledger.OpenPayments.Serialization;

namespace Interledger.OpenPayments.Generated.Resource;

public partial class ResourceServerClient : GeneratedClientBase
{
    public ResourceServerClient(HttpClient httpClient)
        : base(httpClient, OpenPaymentsSerialization.DefaultSettings) { }

    /// <summary>Client wallet address URL of the SDK consumer.</summary>
    public Uri ClientUrl { get; set; } = default!;

    partial void PrepareRequest(HttpClient client, HttpRequestMessage request, string url);

    partial void PrepareRequest(
        HttpClient client,
        HttpRequestMessage request,
        System.Text.StringBuilder urlBuilder
    );

    partial void ProcessResponse(HttpClient client, HttpResponseMessage response);
}

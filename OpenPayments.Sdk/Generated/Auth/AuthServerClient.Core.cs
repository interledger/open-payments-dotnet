using Interledger.OpenPayments.Serialization;

namespace Interledger.OpenPayments.Generated.Auth;

public partial class AuthServerClient : GeneratedClientBase
{
    public AuthServerClient(HttpClient httpClient)
        : base(httpClient, OpenPaymentsSerialization.DefaultSettings) { }

    /// <summary>Client wallet address URL sent as the <c>client</c> field of grant requests.</summary>
    public Uri ClientUrl { get; set; } = default!;

    partial void PrepareRequest(HttpClient client, HttpRequestMessage request, string url);

    partial void PrepareRequest(
        HttpClient client,
        HttpRequestMessage request,
        System.Text.StringBuilder urlBuilder
    );

    partial void ProcessResponse(HttpClient client, HttpResponseMessage response);
}

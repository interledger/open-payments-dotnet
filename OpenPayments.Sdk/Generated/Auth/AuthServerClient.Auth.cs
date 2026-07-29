using Newtonsoft.Json;
using NSec.Cryptography;

namespace OpenPayments.Sdk.Generated.Auth;

/// <summary>
/// Hand-written extensions to the generated <see cref="AuthServerClient"/> that add HTTP Message
/// Signature support and route requests through the client-configured contract resolver.
/// </summary>
public partial class AuthServerClient
{
    private Key? _privateKey;
    private string? _keyId;

    /// <summary>
    /// The client's own wallet address, sent as the <c>client</c> field of grant requests to identify the
    /// requesting client. This is not the server URL — that is set per-call via <c>BaseUrl</c>.
    /// </summary>
    public Uri ClientUrl { get; set; }

    /// <summary>
    /// Sets the key used to sign every subsequent request made by this client.
    /// </summary>
    /// <param name="privateKey">Private key used to sign requests.</param>
    /// <param name="keyId">Key ID sent alongside the signature.</param>
    public void AddSigningKey(Key privateKey, string keyId)
    {
        _privateKey = privateKey;
        _keyId = keyId;
    }

    static partial void UpdateJsonSerializerSettings(JsonSerializerSettings settings)
    {
        settings.ContractResolver = new AuthContractResolver();
    }

    partial void PrepareRequest(HttpClient client, HttpRequestMessage request, string url)
    {
        if (_privateKey == null || _keyId == null)
            return;

        var headers = HttpRequestSigner.SignHttpRequestAsync(request, _privateKey, _keyId).Result;
        request.Headers.TryAddWithoutValidation("Signature", headers.Signature);
        request.Headers.TryAddWithoutValidation("Signature-Input", headers.SignatureInput);
    }
}

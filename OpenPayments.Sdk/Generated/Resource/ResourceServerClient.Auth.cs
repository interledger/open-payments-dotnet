using Newtonsoft.Json;
using NSec.Cryptography;

namespace OpenPayments.Sdk.Generated.Resource;

/// <summary>
/// Hand-written extensions to the generated <see cref="ResourceServerClient"/> that add HTTP Message
/// Signature support and route requests through the client-configured contract resolver.
/// </summary>
public partial class ResourceServerClient
{
    private Key? _privateKey;
    private string? _keyId;

    /// <summary>
    /// The resource server URL that requests are sent to. Set before each call via <c>BaseUrl</c>, since a
    /// single client instance is reused across requests to different resource servers.
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
        settings.ContractResolver = new ResourceContractResolver();
    }

    partial void PrepareRequest(HttpClient client, HttpRequestMessage request, string url)
    {
        if (_privateKey == null || _keyId == null)
        {
            throw new InvalidOperationException("Signing key not set");
        }

        var headers = HttpRequestSigner.SignHttpRequestAsync(request, _privateKey, _keyId).Result;
        request.Headers.TryAddWithoutValidation("Signature", headers.Signature);
        request.Headers.TryAddWithoutValidation("Signature-Input", headers.SignatureInput);
    }
}

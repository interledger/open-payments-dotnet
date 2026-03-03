using Newtonsoft.Json;
using NSec.Cryptography;

namespace OpenPayments.Sdk.Generated.Auth;

public partial class AuthServerClient
{
    private Key? _privateKey;
    private string? _keyId;
    public Uri ClientUrl { get; set; }

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

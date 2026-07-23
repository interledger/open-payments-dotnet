using Newtonsoft.Json;

namespace Interledger.OpenPayments.Generated.Auth;

public partial class AuthServerClient
{
    public Uri ClientUrl { get; set; }

    static partial void UpdateJsonSerializerSettings(JsonSerializerSettings settings)
    {
        settings.ContractResolver = new AuthContractResolver();
    }

    private static string NormalizeBaseUrl(Uri baseUri)
    {
        var value = baseUri.ToString();
        return value.EndsWith("/") ? value : value + "/";
    }
}

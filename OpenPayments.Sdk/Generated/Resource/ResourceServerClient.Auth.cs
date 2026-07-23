using Newtonsoft.Json;

namespace Interledger.OpenPayments.Generated.Resource;

public partial class ResourceServerClient
{
    public Uri ClientUrl { get; set; }

    static partial void UpdateJsonSerializerSettings(JsonSerializerSettings settings)
    {
        settings.ContractResolver = new ResourceContractResolver();
    }

    private static string NormalizeBaseUrl(Uri baseUri)
    {
        var value = baseUri.ToString();
        return value.EndsWith("/") ? value : value + "/";
    }
}

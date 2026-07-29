using Newtonsoft.Json;

namespace OpenPayments.Sdk.Generated.Resource;

public partial class ResourceServerClient
{
    static partial void UpdateJsonSerializerSettings(JsonSerializerSettings settings)
    {
        settings.ContractResolver = new ResourceContractResolver();
    }
}

using Newtonsoft.Json;

namespace OpenPayments.Sdk.Generated.Auth;

public partial class AuthServerClient
{
    static partial void UpdateJsonSerializerSettings(JsonSerializerSettings settings)
    {
        settings.ContractResolver = new AuthContractResolver();
    }
}

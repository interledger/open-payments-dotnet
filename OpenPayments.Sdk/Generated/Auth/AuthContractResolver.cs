using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace OpenPayments.Sdk.Generated.Auth;

public sealed class AuthContractResolver : DefaultContractResolver
{
    protected override JsonProperty CreateProperty(System.Reflection.MemberInfo member,
        MemberSerialization memberSerialization)
    {
        var prop = base.CreateProperty(member, memberSerialization);
        
        // Neutralize Required.Always
        prop.Required = Required.Default;
        // Also avoid throwing on missing/null
        prop.NullValueHandling = NullValueHandling.Ignore;

        return prop;
    }
}
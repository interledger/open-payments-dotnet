using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace OpenPayments.Sdk.Generated.Resource;

public sealed class ResourceContractResolver : DefaultContractResolver
{
    protected override JsonProperty CreateProperty(System.Reflection.MemberInfo member,
        MemberSerialization memberSerialization)
    {
        var prop = base.CreateProperty(member, memberSerialization);
        return prop;
    }
}

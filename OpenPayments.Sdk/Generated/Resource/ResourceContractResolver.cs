using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace OpenPayments.Sdk.Generated.Resource;

public sealed class ResourceContractResolver : DefaultContractResolver
{
    protected override JsonProperty CreateProperty(System.Reflection.MemberInfo member,
        MemberSerialization memberSerialization)
    {
        var prop = base.CreateProperty(member, memberSerialization);
        prop.Required = Required.AllowNull;
        prop.NullValueHandling = NullValueHandling.Ignore;
        
        // Force 'metadata' property to be optional, regardless of what the attributes say
        if (prop.PropertyName != null && prop.PropertyName.Equals("metadata", StringComparison.OrdinalIgnoreCase))
        {
            prop.Required = Required.Default;
            prop.NullValueHandling = NullValueHandling.Ignore;
        }
        
        return prop;
    }
}
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace OpenPayments.Sdk.Generated.Resource;

/// <summary>
/// Newtonsoft.Json contract resolver used when (de)serializing requests and responses for the generated
/// Resource server client.
/// </summary>
public sealed class ResourceContractResolver : DefaultContractResolver
{
    /// <inheritdoc/>
    protected override JsonProperty CreateProperty(System.Reflection.MemberInfo member,
        MemberSerialization memberSerialization)
    {
        var prop = base.CreateProperty(member, memberSerialization);
        return prop;
    }
}

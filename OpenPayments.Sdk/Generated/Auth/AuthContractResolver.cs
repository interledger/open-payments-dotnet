using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace OpenPayments.Sdk.Generated.Auth;

/// <summary>
/// Newtonsoft.Json contract resolver used when (de)serializing requests and responses for the generated
/// Auth server client. Relaxes <see cref="Required.Always"/> constraints and ignores null values so
/// partially populated request/response models don't throw during (de)serialization.
/// </summary>
public sealed class AuthContractResolver : DefaultContractResolver
{
    /// <inheritdoc/>
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

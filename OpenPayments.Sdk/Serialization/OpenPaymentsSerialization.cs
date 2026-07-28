using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace OpenPayments.Sdk.Serialization;

/// <summary>
/// The single contract resolver used for all Open Payments payloads. It relaxes the generated
/// contracts' <see cref="Required.Always"/> constraints and ignores nulls, so minor
/// spec-vs-server drift degrades to default property values instead of failing the whole
/// call — the behavior the auth client always had, now applied uniformly to the resource
/// server, wallet address, and public incoming-payment responses too.
/// </summary>
public sealed class OpenPaymentsContractResolver : DefaultContractResolver
{
    /// <inheritdoc/>
    protected override JsonProperty CreateProperty(
        System.Reflection.MemberInfo member,
        MemberSerialization memberSerialization
    )
    {
        var property = base.CreateProperty(member, memberSerialization);
        property.Required = Required.Default;
        property.NullValueHandling = NullValueHandling.Ignore;

        return property;
    }
}

/// <summary>Shared serializer configuration for every Open Payments client.</summary>
public static class OpenPaymentsSerialization
{
    /// <summary>Serializer settings using <see cref="OpenPaymentsContractResolver"/>.</summary>
    public static JsonSerializerSettings DefaultSettings { get; } =
        new() { ContractResolver = new OpenPaymentsContractResolver() };
}

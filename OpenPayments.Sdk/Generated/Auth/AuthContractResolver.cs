using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace OpenPayments.Sdk.Generated.Auth;

public sealed class AuthContractResolver : DefaultContractResolver
{
    private readonly Dictionary<Type, HashSet<string>> _exclusions = new Dictionary<Type, HashSet<string>>();

    public AuthContractResolver()
    {
        // this.SetProp(typeof(Response), "Interact");
    }

    private void SetProp(Type targetType, params string[] propertyNames)
    {
        if (!_exclusions.ContainsKey(targetType))
        {
            _exclusions.Add(targetType, new HashSet<string>(propertyNames, StringComparer.OrdinalIgnoreCase));
        }

        _exclusions[targetType].UnionWith(propertyNames);
    }

    protected override JsonProperty CreateProperty(System.Reflection.MemberInfo member,
        MemberSerialization memberSerialization)
    {
        var prop = base.CreateProperty(member, memberSerialization);
        //
        // if (prop.DeclaringType == null || !this._exclusions.ContainsKey(prop.DeclaringType) ||
        //     !this._exclusions[prop.DeclaringType].Contains(prop.PropertyName!)) return prop;

        // Neutralize Required.Always
        prop.Required = Required.Default;
        // Also avoid throwing on missing/null
        prop.NullValueHandling = NullValueHandling.Ignore;
        // prop.DefaultValueHandling = DefaultValueHandling.Ignore;

        return prop;
    }
}
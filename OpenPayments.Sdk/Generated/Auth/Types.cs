using System.Collections.ObjectModel;
using Newtonsoft.Json;

namespace OpenPayments.Sdk.Generated.Auth
{
    public partial class GrantCreateBody
    {
        [JsonProperty("access_token")] public AccessToken? AccessToken { get; set; }

        [JsonProperty("client")] public Uri? Client { get; set; }

        [JsonProperty("interact")] public InteractRequest? Interact { get; set; }
    }

    public partial class GrantContinueBody
    {
        [JsonProperty("interact_ref")] public string? InteractRef { get; set; }
    }

    public partial class AccessToken
    {
        [JsonProperty("access")] public Collection<AccessItem>? Access { get; set; }

        [JsonProperty("value")] public string? Value { get; set; }

        [JsonProperty("manage")] public string? Manage { get; set; }

        [JsonProperty("expires_in")] public int? ExpiresIn { get; set; }
    }

    /// <summary>
    /// The access associated with the access token is described using objects that each contain multiple dimensions of access.
    /// </summary>
    public partial class AccessItem
    {
        [JsonProperty("type")] public string? Type { get; set; }

        // [JsonProperty("actions", ItemConverterType = typeof(Converters.StringEnumConverter))]
        [JsonProperty("actions")] public string[]? Actions { get; set; }
        // public System.Collections.Generic.ICollection<Actions> Actions { get; set; } = new System.Collections.ObjectModel.Collection<Actions>();

        [JsonProperty("identifier")] public string? Identifier { get; set; }

        [JsonProperty("limits")] public AccessLimits? Limits { get; set; }
    }

    public partial class AccessLimits
    {
        [JsonProperty("debitAmount")] public Amount? DebitAmount { get; set; }

        [JsonProperty("receiveAmount")] public Amount? ReceiveAmount { get; set; }

        [JsonProperty("interval")] public string? Interval { get; set; }
    }

    public partial class AuthResponse
    {
        [JsonProperty("access_token")] public AccessToken? AccessToken { get; set; }

        [JsonProperty("interact")] public InteractResponse? Interact { get; set; }

        [JsonProperty("continue")] public Continue? Continue { get; set; }
    }

    public partial class ErrorResponse
    {
        [JsonProperty("error", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        public ErrorItem? Error { get; set; }

        private IDictionary<string, object> _additionalProperties;

        [JsonExtensionData]
        public IDictionary<string, object> AdditionalProperties
        {
            get
            {
                return _additionalProperties ??
                       (_additionalProperties = new System.Collections.Generic.Dictionary<string, object>());
            }
            set { _additionalProperties = value; }
        }
    }

    public partial class ErrorItem
    {
        [JsonProperty("description", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        public string? Description { get; set; }

        [JsonProperty("code", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        public ErrorItemCode Code { get; set; }

        private IDictionary<string, object> _additionalProperties;

        [JsonExtensionData]
        public IDictionary<string, object> AdditionalProperties
        {
            get
            {
                return _additionalProperties ??
                       (_additionalProperties = new System.Collections.Generic.Dictionary<string, object>());
            }
            set { _additionalProperties = value; }
        }
    }

    public enum ErrorItemCode
    {
        [System.Runtime.Serialization.EnumMember(Value = @"invalid_client")]
        InvalidClient = 0,

        [System.Runtime.Serialization.EnumMember(Value = @"invalid_request")]
        InvalidRequest = 1,

        [System.Runtime.Serialization.EnumMember(Value = @"request_denied")]
        RequestDenied = 2,

        [System.Runtime.Serialization.EnumMember(Value = @"too_fast")]
        TooFast = 3,

        [System.Runtime.Serialization.EnumMember(Value = @"invalid_continuation")]
        InvalidContinuation = 4,

        [System.Runtime.Serialization.EnumMember(Value = @"invalid_rotation")]
        InvalidRotation = 5,
    }
}
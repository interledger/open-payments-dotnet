using System.Collections.ObjectModel;
using Newtonsoft.Json;

namespace OpenPayments.Sdk.Generated.Auth
{
    public partial class GrantCreateBody
    {
        [JsonProperty("access_token")] public AccessToken? AccessToken { get; set; }

        [JsonProperty("client")] public Uri? Client { get; set; }

        [JsonProperty("interact")] public InteractRequest? Interact { get; set; }

        // TODO: Add subject
    }

    public partial class GrantContinueBody
    {
        [JsonProperty("interact_ref")] public string? InteractRef { get; set; }
    }

    public partial class AccessToken
    {
        [JsonProperty("access")] public required Collection<AccessItem> Access { get; set; }

        [JsonProperty("value")] public string Value { get; set; } = null!;

        [JsonProperty("manage")] public string Manage { get; set; } = null!;

        [JsonProperty("expires_in")] public int? ExpiresIn { get; set; }
    }

    /// <summary>
    /// The access associated with the access token is described using objects that each contain multiple dimensions of access.
    /// </summary>
    public partial class AccessItem
    {
        [JsonProperty("type")]
        [System.ComponentModel.DataAnnotations.Required(AllowEmptyStrings = true)]
        [Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        public AccessType Type { get; set; }

        [JsonProperty("actions", ItemConverterType = typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        [System.ComponentModel.DataAnnotations.Required]
        // [JsonProperty("actions")] public string[]? Actions { get; set; }
        public ICollection<Actions> Actions { get; set; } = new Collection<Actions>();

        [JsonProperty("identifier")] public string? Identifier { get; set; }

        [JsonProperty("limits")] public AccessLimits? Limits { get; set; }
    }
    
    public enum AccessType
    {
        [System.Runtime.Serialization.EnumMember(Value = @"incoming-payment")]
        IncomingPayment = 0,
        [System.Runtime.Serialization.EnumMember(Value = @"outgoing-payment")]
        OutgoingPayment = 1,
        [System.Runtime.Serialization.EnumMember(Value = @"quote")]
        Quote = 2,
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

        [JsonProperty("continue")] public AuthContinue Continue { get; set; } = null!;
    }

    public partial class AuthContinue
    {
        /// <summary>
        /// A unique access token for continuing the request, called the "continuation access token".
        /// </summary>
        [JsonProperty("access_token", Required = Required.Always)]
        [System.ComponentModel.DataAnnotations.Required]
        public Access_token2 AccessToken { get; set; } = new Access_token2();

        /// <summary>
        /// The URI at which the client instance can make continuation requests.
        /// </summary>
        [JsonProperty("uri", Required = Required.Always)]
        [System.ComponentModel.DataAnnotations.Required(AllowEmptyStrings = true)]
        public Uri Uri { get; set; } = null!;

        /// <summary>
        /// The amount of time in integer seconds the client instance MUST wait after receiving this request continuation response and calling the continuation URI.
        /// </summary>
        [JsonProperty("wait", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        public int Wait { get; set; }

        private IDictionary<string, object> _additionalProperties;

        [JsonExtensionData]
        public IDictionary<string, object> AdditionalProperties
        {
            get { return _additionalProperties ?? (_additionalProperties = new System.Collections.Generic.Dictionary<string, object>()); }
            set { _additionalProperties = value; }
        }
    }

    public partial class RotateTokenResponse
    {
        [JsonProperty("access_token")] public required AccessToken AccessToken { get; set; }
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

    public partial class Amount
    {
        public Amount() {}
        public Amount(string value, string assetCode, int? assetScale = 2)
        {
            this.Value = value;
            this.AssetCode = assetCode;
            this.AssetScale = assetScale ?? 2;
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
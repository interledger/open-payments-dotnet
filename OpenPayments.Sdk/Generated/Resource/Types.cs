using Newtonsoft.Json;

namespace OpenPayments.Sdk.Generated.Resource
{
    public partial class IncomingPaymentBody : Body
    {
        /// <summary>
        /// The date and time when payments into the incoming payment must no longer be accepted.
        /// </summary>
        [JsonProperty("expiresAt", Required = Required.AllowNull, NullValueHandling = NullValueHandling.Ignore)]
        public new DateTimeOffset? ExpiresAt { get; set; }

        /// <summary>
        /// Additional metadata associated with the incoming payment. (Optional)
        /// </summary>
        [JsonProperty("metadata", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        public new object? Metadata { get; set; }
    }

    public partial class IncomingPaymentResponse : IncomingPaymentWithMethods
    {
        /// <inheritdoc cref="IncomingPayment.Metadata"/>
        [JsonProperty("metadata", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
        public new object? Metadata { get; set; }
    }

    public partial class ListIncomingPaymentQuery
    {
        public required string WalletAddress { get; set; }
        public string? Cursor { get; set; }
        public int? First { get; set; }
        public int? Last { get; set; }
    }

    public partial class ListIncomingPaymentsResponse : Response
    {
    }

    public partial class QuoteBody : Body3
    {
    }

    public partial class QuoteBodyWithDebitAmount : QuoteBody
    {
        /// <summary>
        /// The fixed amount that would be sent from the sending wallet address given a successful outgoing payment.
        /// </summary>
        [JsonProperty("debitAmount")]
        public required Amount DebitAmount { get; set; }
    }

    public partial class QuoteBodyWithReceiveAmount : QuoteBody
    {
        /// <summary>
        /// The fixed amount that would be paid into the receiving wallet address given a successful outgoing payment.
        /// </summary>
        [JsonProperty("receiveAmount")]
        public required Amount ReceiveAmount { get; set; }
    }

    public partial class QuoteResponse : Quote
    {
    }

    public abstract class OutgoingPaymentBody
    {
        [JsonProperty("walletAddress", Required = Required.Always)]
        [System.ComponentModel.DataAnnotations.Required(AllowEmptyStrings = true)]
        public Uri WalletAddress { get; set; } = default!;

        /// <inheritdoc cref="Body2.Metadata"/>
        [JsonProperty("metadata", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
        public object? Metadata { get; set; }
    }

    public partial class OutgoingPaymentBodyFromQuote : OutgoingPaymentBody
    {
        /// <summary>
        /// The URL of the Quote defining this payment's amounts.
        /// </summary>
        [JsonProperty("quoteId", Required = Required.AllowNull)]
        public required Uri QuoteId { get; set; }
    }

    public partial class OutgoingPaymentBodyFromIncomingPayment : OutgoingPaymentBody
    {
        /// <summary>
        /// The URL of the incoming payment this outgoing payment will fulfill.
        /// </summary>
        [JsonProperty("incomingPayment")]
        public required Uri IncomingPayment { get; set; }

        /// <summary>
        /// The fixed amount that would be sent from the sending wallet address given a successful outgoing payment.
        /// </summary>
        [JsonProperty("debitAmount")]
        public required Amount DebitAmount { get; set; }
    }

    public partial class OutgoingPaymentResponse : OutgoingPayment
    {
    }

    public partial class OutgoingPaymentWithSpentAmountsResponse : OutgoingPaymentWithSpentAmounts
    {
    }

    public partial class ListOutgoingPaymentQuery
    {
        public required string WalletAddress { get; set; }
        public string? Cursor { get; set; }
        public int? First { get; set; }
        public int? Last { get; set; }
    }

    public partial class ListOutgoingPaymentsResponse : Response2
    {
    }

    public partial class Amount
    {
        public Amount()
        {
        }

        public Amount(string value, string assetCode, int? assetScale = 2)
        {
            Value = value;
            AssetCode = assetCode;
            AssetScale = assetScale ?? 2;
        }
    }

    public partial class ResourceErrorResponse
    {
        [JsonProperty("error", Required = Required.Always)]
        [System.ComponentModel.DataAnnotations.Required]
        public ResourceError Error { get; set; } = new ResourceError();

        private IDictionary<string, object>? _additionalProperties;

        [JsonExtensionData]
        public IDictionary<string, object> AdditionalProperties
        {
            get { return _additionalProperties ?? (_additionalProperties = new Dictionary<string, object>()); }
            set { _additionalProperties = value; }
        }
    }

    public partial class ResourceError
    {
        [JsonProperty("code", Required = Required.Always)]
        [System.ComponentModel.DataAnnotations.Required(AllowEmptyStrings = true)]
        public string Code { get; set; } = null!;

        [JsonProperty("description", Required = Required.Always)]
        [System.ComponentModel.DataAnnotations.Required(AllowEmptyStrings = true)]
        public string Description { get; set; } = null!;

        /// <summary>
        /// Additional details about the error.
        /// </summary>
        [JsonProperty("details", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
        public object Details { get; set; } = null!;

        private IDictionary<string, object>? _additionalProperties;

        [JsonExtensionData]
        public IDictionary<string, object> AdditionalProperties
        {
            get { return _additionalProperties ?? (_additionalProperties = new Dictionary<string, object>()); }
            set { _additionalProperties = value; }
        }
    }
}

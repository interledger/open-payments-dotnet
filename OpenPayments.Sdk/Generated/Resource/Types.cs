using Newtonsoft.Json;

namespace OpenPayments.Sdk.Generated.Resource
{
    public partial class IncomingPaymentBody : Body
    {
        /// <summary>
        /// The date and time when payments into the incoming payment must no longer be accepted.
        /// </summary>
        [JsonProperty(
            "expiresAt",
            Required = Required.AllowNull,
            NullValueHandling = NullValueHandling.Ignore
        )]
        public new DateTimeOffset? ExpiresAt { get; set; }

        /// <summary>
        /// Additional metadata associated with the incoming payment. (Optional)
        /// </summary>
        [JsonProperty(
            "metadata",
            Required = Required.DisallowNull,
            NullValueHandling = NullValueHandling.Ignore
        )]
        public new object? Metadata { get; set; }
    }

    public partial class IncomingPaymentResponse : IncomingPaymentWithMethods
    {
        /// <inheritdoc cref="IncomingPayment.Metadata"/>
        [JsonProperty(
            "metadata",
            Required = Required.Default,
            NullValueHandling = NullValueHandling.Ignore
        )]
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
        /// <summary>
        /// The fixed amount that would be paid into the receiving wallet address given a successful outgoing payment.
        /// </summary>
        [JsonProperty("receiveAmount")]
        public Amount? ReceiveAmount { get; set; }

        /// <summary>
        /// The fixed amount that would be sent from the sending wallet address given a successful outgoing payment.
        /// </summary>
        [JsonProperty("debitAmount")]
        public Amount? DebitAmount { get; set; }
    }

    public partial class QuoteResponse : Quote
    {
    }

    public partial class OutgoingPaymentBody : Body2
    {
        /// <summary>
        /// The URL of the quote defining this payment's amounts.
        /// </summary>
        [JsonProperty("quoteId", Required = Required.AllowNull)]
        public new Uri? QuoteId { get; set; }

        /// <summary>
        /// The URL of the incoming payment this outgoing payment will fulfill.
        /// </summary>
        [JsonProperty("incomingPayment")]
        public Uri? IncomingPayment { get; set; }

        /// <summary>
        /// The fixed amount that would be sent from the sending wallet address given a successful outgoing payment.
        /// </summary>
        [JsonProperty("debitAmount")]
        public Amount? DebitAmount { get; set; }

        /// <inheritdoc cref="Body2.Metadata"/>
        [JsonProperty(
            "metadata",
            Required = Required.Default,
            NullValueHandling = NullValueHandling.Ignore
        )]
        public new object? Metadata { get; set; }
    }

    public partial class OutgoingPaymentResponse : OutgoingPaymentWithSpentAmounts
    {
    }

    public partial class Amount
    {
        public Amount()
        {
        }

        public Amount(string value, string assetCode, int? assetScale = 2)
        {
            this.Value = value;
            this.AssetCode = assetCode;
            this.AssetScale = assetScale ?? 2;
        }
    }
}

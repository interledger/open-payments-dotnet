using AwesomeAssertions;
using Newtonsoft.Json;
using OpenPayments.Sdk.Generated.Auth;
using OpenPayments.Sdk.Serialization;

namespace OpenPayments.Sdk.Tests.Clients;

public class SerializationNullOmission_Tests
{
    // OutgoingAccessLimits (nested under a grant request's AccessToken.Access[].Limits) declares
    // its optional properties with plain [JsonProperty("...")] attributes — no NullValueHandling
    // set on the property itself. Left to Json.NET's own default, a null-valued property here
    // would serialize as `"interval": null`. The shared OpenPaymentsContractResolver forces
    // NullValueHandling.Ignore on every property, so it must be the resolver — not the DTO's own
    // attributes — that omits it from request bodies too, not just response deserialization.
    [Fact]
    public void OutgoingAccessLimits_NullOptionalProperties_AreOmittedFromRequestBody()
    {
        var limits = new OutgoingAccessLimits
        {
            Receiver = "https://host-b.example/incoming-payments/1",
            Interval = null,
            DebitAmount = null,
            ReceiveAmount = new Amount("100", "EUR", 2),
        };

        var json = JsonConvert.SerializeObject(limits, OpenPaymentsSerialization.DefaultSettings);

        json.Should().Contain("\"receiver\"");
        json.Should().Contain("\"receiveAmount\"");
        json.Should().NotContain("\"interval\"");
        json.Should().NotContain("\"debitAmount\"");
    }
}

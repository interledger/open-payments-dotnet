namespace OpenPayments.Sdk.Configuration;

/// <summary>
/// Names of the <see cref="System.Net.Http.IHttpClientFactory"/> clients the SDK registers.
/// </summary>
internal static class OpenPaymentsHttpClients
{
    /// <summary>Pipeline with the signing handler, for auth-server and resource-server calls.</summary>
    internal const string Signed = "openpayments-signed";

    /// <summary>Pipeline without signing, for wallet-address and public incoming-payment reads.</summary>
    internal const string Unsigned = "openpayments";
}

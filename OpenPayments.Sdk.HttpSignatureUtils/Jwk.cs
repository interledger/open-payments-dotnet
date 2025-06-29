using Newtonsoft.Json;

namespace OpenPayments.Sdk.HttpSignatureUtils;

/// <summary>
/// Minimal JSON Web Key (JWK) representation for Ed25519 keys used in Open Payments HTTP signatures.
/// Only the parameters required by the spec are included.
/// </summary>
public sealed class Jwk
{
    [JsonProperty("kty")] public string Kty { get; init; } = "OKP"; // Octet Key Pair
    [JsonProperty("crv")] public string Crv { get; init; } = "Ed25519";
    [JsonProperty("alg")] public string Alg { get; init; } = "EdDSA";

    /// <summary>Base64‐url encoded public key bytes (32 bytes).</summary>
    [JsonProperty("x")] public required string X { get; init; }

    /// <summary>Base64‐url encoded private key bytes (32 bytes). Optional—omit for public-only keys.</summary>
    [JsonProperty("d", NullValueHandling = NullValueHandling.Ignore)] public string? D { get; init; }

    /// <summary>Key identifier.</summary>
    [JsonProperty("kid", NullValueHandling = NullValueHandling.Ignore)] public required string Kid { get; init; }
} 
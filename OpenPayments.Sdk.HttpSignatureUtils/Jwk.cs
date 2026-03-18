using Newtonsoft.Json;

namespace OpenPayments.Sdk.HttpSignatureUtils;

/// <summary>
/// Minimal JSON Web Key (JWK) representation for Ed25519 keys used in Open Payments HTTP signatures.
/// Only the parameters required by the spec are included.
/// </summary>
public sealed class Jwk
{
    /// <summary>
    /// The key type parameter of the JSON Web Key (JWK).
    /// Specifies the category of the key, initialized as "OKP" (Octet Key Pair)
    /// for representing cryptographic keys used in HTTP Signature validation.
    /// </summary>
    [JsonProperty("kty")]
    public string Kty { get; init; } = "OKP"; // Octet Key Pair

    /// <summary>
    /// The cryptographic curve parameter associated with the key.
    /// Specifies the curve used, initialized as "Ed25519" for elliptic curve cryptography.
    /// </summary>
    [JsonProperty("crv")]
    public string Crv { get; init; } = "Ed25519";

    /// <summary>
    /// Specifies the algorithm parameter for the JSON Web Key (JWK).
    /// Defaults to "EdDSA", indicating the Edwards-curve Digital Signature Algorithm.
    /// </summary>
    [JsonProperty("alg")]
    public string Alg { get; init; } = "EdDSA";

    /// <summary>Base64‐url encoded public key bytes (32 bytes).</summary>
    [JsonProperty("x")]
    public required string X { get; init; }

    /// <summary>Base64‐url encoded private key bytes (32 bytes). Optional—omit for public-only keys.</summary>
    [JsonProperty("d", NullValueHandling = NullValueHandling.Ignore)]
    public string? D { get; init; }

    /// <summary>Key identifier.</summary>
    [JsonProperty("kid", NullValueHandling = NullValueHandling.Ignore)]
    public required string Kid { get; init; }
}

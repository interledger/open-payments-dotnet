using OpenPayments.Sdk.HttpSignatureUtils;

/// <summary>
/// Defines methods for validating HTTP signatures in requests.
/// Implementations of this interface ensure the integrity and authenticity of incoming HTTP requests
/// by verifying their signatures against provided public keys and specified signature inputs.
/// </summary>
public interface IHttpSignatureValidator
{
    /// <summary>
    /// Validates the HTTP signature of an incoming request using the provided client key.
    /// Ensures that the integrity and authenticity of the request are maintained by verifying
    /// its signature against the provided Ed25519 public key and its associated signature input components.
    /// </summary>
    /// <param name="request">The HTTP request message containing the signature to validate.</param>
    /// <param name="clientKey">The JSON Web Key (JWK) representing the client's public key used for validation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating whether the signature is valid.</returns>
    Task<bool> ValidateSignatureAsync(HttpRequestMessage request, Jwk clientKey);

    /// <summary>
    /// Checks if the signature headers are present in the request.
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    bool AreSignatureHeadersPresent(HttpRequestMessage request);
}

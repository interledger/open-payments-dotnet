/// <summary>
/// Defines the contract for a signature input validator, responsible for validating
/// the components of the signature input against an HTTP request.
/// </summary>
public interface ISignatureInputValidator
{
    /// <summary>
    /// Validates the provided signature input components against the specified HTTP request
    /// to ensure they meet the required criteria for signature generation or verification.
    /// </summary>
    /// <param name="components">
    /// A list of signature input components, specifying the parts of the HTTP request to include in the signature.
    /// </param>
    /// <param name="request">
    /// The <see cref="HttpRequestMessage"/> representing the HTTP request to validate against the components.
    /// </param>
    /// <returns>
    /// A boolean value indicating whether the signature input components are valid for the given request.
    /// </returns>
    bool Validate(List<string> components, HttpRequestMessage request);
}

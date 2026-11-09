using OpenPayments.Sdk.HttpSignatureUtils;

/// <inheritdoc cref="ISignatureInputBuilder"/>
public class SignatureInputBuilder : ISignatureInputBuilder
{
    private const string Label = "sig1=";

    /// <inheritdoc cref="ISignatureInputBuilder"/>
    public Task<string?> BuildBaseAsync(
        List<string> components,
        HttpRequestMessage request,
        string sigInput
    )
    {
        // RFC 9421 requires the received parameters verbatim, so they are echoed rather than
        // rebuilt. Only the sig1= label is stripped.
        var signatureParams = sigInput.StartsWith(Label, StringComparison.Ordinal)
            ? sigInput[Label.Length..]
            : sigInput;

        // Not async: the shared builder reads headers only, so there is nothing to await. The
        // Task-returning signature is kept because it is public API.
        return Task.FromResult<string?>(
            SignatureBaseBuilder.Build(components, signatureParams, request)
        );
    }
}

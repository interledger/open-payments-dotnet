using OpenPayments.Sdk.HttpSignatureUtils;

/// <inheritdoc cref="ISignatureInputValidator"/>
public class SignatureInputValidator : ISignatureInputValidator
{
    /// <inheritdoc cref="ISignatureInputValidator"/>
    public bool Validate(List<string> components, HttpRequestMessage request)
    {
        if (components.Any(c => !c.Equals(c, StringComparison.CurrentCultureIgnoreCase)))
            return false;

        var hasMethod = components.Contains("@method");
        var hasTargetUri = components.Contains("@target-uri");
        var hasAuth =
            !request.Headers.Contains("Authorization") || components.Contains("authorization");

        var hasDigest =
            !components.Contains("content-digest")
            || request.Content != null
            && request.Content.Headers.Contains("Content-Digest")
            && request.Content.Headers.Contains("Content-Length")
            && request.Content.Headers.Contains("Content-Type");

        return hasMethod && hasTargetUri && hasAuth && hasDigest;
    }
}

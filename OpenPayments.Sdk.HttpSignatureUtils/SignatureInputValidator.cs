using OpenPayments.Sdk.HttpSignatureUtils;

/// <inheritdoc cref="ISignatureInputValidator"/>
public class SignatureInputValidator : ISignatureInputValidator
{
    /// <inheritdoc cref="ISignatureInputValidator"/>
    public bool Validate(List<string> components, HttpRequestMessage request)
    {
        // RFC 9421 section 2.1: component names are lowercase. Comparing ordinally against the
        // lowercased form is the check that was intended; comparing c to itself always passed.
        if (components.Any(c => !string.Equals(c, c.ToLowerInvariant(), StringComparison.Ordinal)))
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

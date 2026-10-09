using OpenPayments.Sdk.HttpSignatureUtils;

/// <inheritdoc cref="ISignatureInputValidator"/>
public class SignatureInputValidator : ISignatureInputValidator
{
    /// <inheritdoc cref="ISignatureInputValidator"/>
    public bool Validate(List<string> components, HttpRequestMessage request)
    {
        // Open Payments / HTTP Message Signatures: component names are lowercase.
        if (components.Any(c => c != c.ToLowerInvariant()))
            return false;

        var hasMethod = components.Contains("@method");
        var hasTargetUri = components.Contains("@target-uri");
        var hasAuth =
            !request.Headers.Contains("Authorization") || components.Contains("authorization");

        // Open Payments / GNAP: when a request body is present, content-digest MUST be
        // covered (parity with open-payments-go / node / php / rust). Previously
        // `!components.Contains("content-digest") || …` treated omission as success.
        var hasBody = RequestHasBody(request);
        var hasDigest =
            !hasBody
            || (
                components.Contains("content-digest")
                && request.Content != null
                && request.Content.Headers.Contains("Content-Digest")
                && request.Content.Headers.Contains("Content-Length")
                && request.Content.Headers.Contains("Content-Type")
            );

        return hasMethod && hasTargetUri && hasAuth && hasDigest;
    }

    private static bool RequestHasBody(HttpRequestMessage request)
    {
        if (request.Content is null)
            return false;

        if (request.Content.Headers.ContentLength is long len)
            return len > 0;

        // Content present without an explicit length: treat as a body so digest
        // coverage cannot be skipped by omitting Content-Length.
        return true;
    }
}

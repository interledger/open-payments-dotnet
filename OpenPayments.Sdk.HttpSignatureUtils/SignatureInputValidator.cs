using OpenPayments.Sdk.HttpSignatureUtils;

internal class SignatureInputValidator : ISignatureInputValidator
{
    public bool Validate(List<string> components, HttpRequestMessage request)
    {
        if (components.Any(c => !c.Equals(c, StringComparison.CurrentCultureIgnoreCase)))
            return false;

        bool hasMethod = components.Contains("@method");
        bool hasTargetUri = components.Contains("@target-uri");
        bool hasAuth = !request.Headers.Contains("Authorization") || components.Contains("authorization");

        bool hasDigest = !components.Contains("content-digest") || request.Content != null &&
              request.Content.Headers.Contains("Content-Digest") &&
              request.Content.Headers.Contains("Content-Length") &&
              request.Content.Headers.Contains("Content-Type");

        return hasMethod && hasTargetUri && hasAuth && hasDigest;
    }
}
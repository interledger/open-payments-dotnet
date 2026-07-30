namespace OpenPayments.Sdk.HttpSignatureUtils;

/// <summary>
/// Builds the RFC 9421 signature base string. This is the single source of truth shared by
/// <see cref="HttpRequestSigner"/> and <see cref="SignatureInputBuilder"/>. When the signer and the
/// validator derived it separately they disagreed, so every signature this library produced was
/// rejected by the validator it ships with (issue #19).
/// </summary>
internal static class SignatureBaseBuilder
{
    /// <summary>
    /// Builds the signature base string for the given covered components.
    /// </summary>
    /// <param name="components">
    /// Ordered covered component identifiers, e.g. <c>@method</c>, <c>content-digest</c>.
    /// </param>
    /// <param name="signatureParams">
    /// The signature parameters as they appear in the <c>Signature-Input</c> header value with the
    /// <c>sig1=</c> label removed, e.g.
    /// <c>("@method" "@target-uri");created=1700000000;keyid="k";alg="ed25519"</c>.
    /// The signer derives this; the validator echoes what it received, because RFC 9421 requires the
    /// parameters to be reproduced verbatim including their ordering.
    /// </param>
    /// <param name="request">The request component values are read from.</param>
    /// <returns>The signature base string, LF-separated.</returns>
    internal static string Build(
        IReadOnlyList<string> components,
        string signatureParams,
        HttpRequestMessage request
    )
    {
        var lines = new List<string>(components.Count + 1);

        foreach (var component in components)
        {
            switch (component)
            {
                case "@method":
                    // RFC 9421 section 2.2.1: the method is case-sensitive and uppercase.
                    lines.Add($"\"@method\": {request.Method.Method.ToUpperInvariant()}");
                    break;
                case "@target-uri":
                    lines.Add($"\"@target-uri\": {request.RequestUri}");
                    break;
                default:
                    lines.Add(
                        $"\"{component.ToLowerInvariant()}\": {GetHeaderValue(request, component)}"
                    );
                    break;
            }
        }

        lines.Add($"\"@signature-params\": {signatureParams}");

        // LF only. Environment.NewLine would make the base string platform-dependent, so a request
        // signed on Windows could not be validated on Linux.
        return string.Join("\n", lines);
    }

    private static string GetHeaderValue(HttpRequestMessage request, string name)
    {
        if (request.Headers.TryGetValues(name, out var values))
            return string.Join(", ", values);

        if (request.Content?.Headers.TryGetValues(name, out var contentValues) == true)
            return string.Join(", ", contentValues);

        return "";
    }
}

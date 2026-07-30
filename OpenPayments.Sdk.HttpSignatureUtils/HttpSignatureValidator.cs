using System.Text;
using NSec.Cryptography;
using OpenPayments.Sdk.HttpSignatureUtils;

/// <inheritdoc cref="IHttpSignatureValidator"/>
public class HttpSignatureValidator : IHttpSignatureValidator
{
    private readonly ISignatureInputParser _parser;
    private readonly ISignatureInputValidator _validator;
    private readonly ISignatureInputBuilder _builder;

    /// <inheritdoc cref="HttpSignatureValidator"/>
    public HttpSignatureValidator(
        ISignatureInputParser parser,
        ISignatureInputValidator validator,
        ISignatureInputBuilder builder
    )
    {
        _parser = parser;
        _validator = validator;
        _builder = builder;
    }

    /// <inheritdoc cref="HttpSignatureValidator"/>
    public bool AreSignatureHeadersPresent(HttpRequestMessage request)
    {
        return TryGetHeader(request, "signature") is not null
            && TryGetHeader(request, "signature-input") is not null;
    }

    /// <inheritdoc cref="HttpSignatureValidator"/>
    public async Task<bool> ValidateSignatureAsync(HttpRequestMessage request, Jwk clientKey)
    {
        var sig = TryGetHeader(request, "signature")!;
        var sigInput = TryGetHeader(request, "signature-input")!;

        var components = _parser.GetComponents(sigInput);
        if (components is null)
            return false;

        if (!_validator.Validate(components, request))
            return false;

        // Checked before the Ed25519 verification so a tampered payload is rejected without doing
        // asymmetric crypto. The signed digest is worthless unless it is compared to the body.
        if (
            components.Contains("content-digest")
            && !await ContentDigestVerifier.MatchesBodyAsync(request).ConfigureAwait(false)
        )
            return false;

        var challenge = await _builder.BuildBaseAsync(components, request, sigInput);
        if (challenge is null)
            return false;

        var signatureBytes = TryParseSignature(sig);
        if (signatureBytes is null)
            return false;
        var publicKey = PublicKey.Import(
            SignatureAlgorithm.Ed25519,
            Base64UrlDecode(clientKey.X),
            KeyBlobFormat.RawPublicKey
        );

        return SignatureAlgorithm.Ed25519.Verify(
            publicKey,
            Encoding.UTF8.GetBytes(challenge),
            signatureBytes
        );
    }

    private static string? TryGetHeader(HttpRequestMessage request, string name)
    {
        if (request.Headers.TryGetValues(name, out var values))
            return values.FirstOrDefault();
        if (request.Content?.Headers.TryGetValues(name, out var cvalues) == true)
            return cvalues.FirstOrDefault();
        return null;
    }

    /// <summary>
    /// Extracts the raw signature from a <c>sig1=:&lt;base64&gt;:</c> header value. Returns null for
    /// anything malformed: the header is attacker-controlled, so a bad value is a failed validation
    /// rather than an exception.
    /// </summary>
    private static byte[]? TryParseSignature(string signatureHeader)
    {
        const string label = "sig1=:";

        var labelIndex = signatureHeader.IndexOf(label, StringComparison.Ordinal);
        if (labelIndex < 0)
            return null;

        var remainder = signatureHeader[(labelIndex + label.Length)..];
        var end = remainder.IndexOf(':');
        if (end <= 0)
            return null;

        var encoded = remainder[..end];
        var buffer = new byte[(encoded.Length * 3 / 4) + 3];

        return Convert.TryFromBase64String(encoded, buffer, out var written)
            ? buffer[..written]
            : null;
    }

    private static byte[] Base64UrlDecode(string input)
    {
        string padded = input.Replace('-', '+').Replace('_', '/');
        return Convert.FromBase64String(
            padded.PadRight(padded.Length + (4 - padded.Length % 4) % 4, '=')
        );
    }
}

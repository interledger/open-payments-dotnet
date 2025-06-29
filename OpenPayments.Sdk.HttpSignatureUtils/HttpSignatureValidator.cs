using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using NSec.Cryptography;

using OpenPayments.Sdk.HttpSignatureUtils;

internal class HttpSignatureValidator : IHttpSignatureValidator
{
    private readonly ISignatureInputParser _parser;
    private readonly ISignatureInputValidator _validator;
    private readonly ISignatureInputBuilder _builder;

    public HttpSignatureValidator(
        ISignatureInputParser parser,
        ISignatureInputValidator validator,
        ISignatureInputBuilder builder)
    {
        _parser = parser;
        _validator = validator;
        _builder = builder;
    }

    public bool AreSignatureHeadersPresent(HttpRequestMessage request)
    {
        return TryGetHeader(request, "signature") is not null &&
               TryGetHeader(request, "signature-input") is not null;
    }

    public async Task<bool> ValidateSignatureAsync(HttpRequestMessage request, Jwk clientKey)
    {
        var sig = TryGetHeader(request, "signature")!;
        var sigInput = TryGetHeader(request, "signature-input")!;

        var components = _parser.GetComponents(sigInput);
        if (components is null)
            return false;

        if (!_validator.Validate(components, request))
            return false;

        var challenge = await _builder.BuildBaseAsync(components, request, sigInput);
        if (challenge is null)
            return false;

        var signatureBytes = Convert.FromBase64String(sig.Replace("sig1=", "").Replace(":", ""));
        var publicKey = PublicKey.Import(SignatureAlgorithm.Ed25519, Base64UrlDecode(clientKey.X), KeyBlobFormat.RawPublicKey);

        return SignatureAlgorithm.Ed25519.Verify(publicKey, Encoding.UTF8.GetBytes(challenge), signatureBytes);
    }

    private static string? TryGetHeader(HttpRequestMessage request, string name)
    {
        if (request.Headers.TryGetValues(name, out var values))
            return values.FirstOrDefault();
        if (request.Content?.Headers.TryGetValues(name, out var cvalues) == true)
            return cvalues.FirstOrDefault();
        return null;
    }

    private static byte[] Base64UrlDecode(string input)
    {
        string padded = input.Replace('-', '+').Replace('_', '/');
        return Convert.FromBase64String(padded.PadRight(padded.Length + (4 - padded.Length % 4) % 4, '='));
    }
}
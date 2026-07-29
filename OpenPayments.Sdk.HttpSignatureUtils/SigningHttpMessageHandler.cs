using NSec.Cryptography;

namespace OpenPayments.Sdk.HttpSignatureUtils;

/// <summary>
/// Signs every outgoing request with an HTTP Message Signature before passing it down the
/// handler pipeline. Register this on the signed <see cref="HttpClient"/> only: unauthenticated
/// Open Payments endpoints must not receive the client's key id.
/// </summary>
public sealed class SigningHttpMessageHandler : DelegatingHandler
{
    private readonly Key _privateKey;
    private readonly string _keyId;

    /// <summary>
    /// Creates a handler that signs with the given Ed25519 key.
    /// </summary>
    /// <param name="privateKey">
    /// Private key used to sign. Not owned by this handler and never disposed by it:
    /// <see cref="IHttpClientFactory"/> rotates handler instances, and a disposed key would
    /// break every request after the first rotation.
    /// </param>
    /// <param name="keyId">Key ID advertised in the <c>Signature-Input</c> header.</param>
    public SigningHttpMessageHandler(Key privateKey, string keyId)
    {
        ArgumentNullException.ThrowIfNull(privateKey);
        if (string.IsNullOrWhiteSpace(keyId))
            throw new ArgumentException("KeyId cannot be empty.", nameof(keyId));

        _privateKey = privateKey;
        _keyId = keyId;
    }

    /// <inheritdoc/>
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Content != null)
        {
            // Buffer so signing cannot consume the stream the inner handler still needs.
            // net8.0 has no CancellationToken overload of LoadIntoBufferAsync.
            await request.Content.LoadIntoBufferAsync().ConfigureAwait(false);

            // The signer appends Content-Digest rather than replacing it, so a re-sent
            // request would otherwise carry two and be signed over the wrong base.
            request.Content.Headers.Remove("Content-Digest");
        }

        request.Headers.Remove("Signature");
        request.Headers.Remove("Signature-Input");

        var headers = await HttpRequestSigner
            .SignHttpRequestAsync(request, _privateKey, _keyId)
            .ConfigureAwait(false);

        request.Headers.TryAddWithoutValidation("Signature", headers.Signature);
        request.Headers.TryAddWithoutValidation("Signature-Input", headers.SignatureInput);

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}

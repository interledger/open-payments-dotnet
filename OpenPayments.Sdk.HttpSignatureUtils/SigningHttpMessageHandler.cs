using NSec.Cryptography;

namespace OpenPayments.Sdk.HttpSignatureUtils;

/// <summary>
/// A <see cref="DelegatingHandler"/> that signs every outgoing request using HTTP Message
/// Signatures before forwarding it to the inner handler. Replaces the sync-over-async
/// <c>PrepareRequest</c> hook previously used by the generated clients.
/// </summary>
public sealed class SigningHttpMessageHandler(Key privateKey, string keyId) : DelegatingHandler
{
    /// <summary>
    /// Signs the outgoing HTTP request with signature headers before forwarding to the inner handler.
    /// </summary>
    /// <param name="request">The HTTP request message to sign.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation that returns the HTTP response message.</returns>
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        var headers = await HttpRequestSigner
            .SignHttpRequestAsync(request, privateKey, keyId)
            .ConfigureAwait(false);

        request.Headers.TryAddWithoutValidation("Signature", headers.Signature);
        request.Headers.TryAddWithoutValidation("Signature-Input", headers.SignatureInput);

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}

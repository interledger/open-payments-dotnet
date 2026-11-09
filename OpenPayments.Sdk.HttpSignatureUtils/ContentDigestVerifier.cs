using System.Security.Cryptography;

namespace OpenPayments.Sdk.HttpSignatureUtils;

/// <summary>
/// Checks the <c>Content-Digest</c> header against the request body. Signing a digest is only
/// meaningful if it is compared to the payload: without this, a request whose body is replaced while
/// the original content headers are replayed produces an identical signature base string and
/// verifies successfully.
/// </summary>
internal static class ContentDigestVerifier
{
    /// <summary>
    /// Returns true when the header carries at least one digest in a recognised algorithm and every
    /// recognised digest matches the body. Fails closed: a missing header, an absent body, or a
    /// header naming only unrecognised algorithms all return false.
    /// </summary>
    internal static async Task<bool> MatchesBodyAsync(HttpRequestMessage request)
    {
        if (request.Content is null)
            return false;

        // Resolved the same way SignatureBaseBuilder resolves it for the signed base string (request
        // headers first, content headers second). Reading content headers directly here let an
        // attacker put the original digest on a request header (satisfying the signature, since that
        // is what the base string sees) and a digest for a different body on the content header
        // (satisfying a verifier that only looked there), forging an arbitrary body under a valid
        // signature. An empty string means the header is absent either way; fail closed.
        var headerValue = SignatureBaseBuilder.GetHeaderValue(request, "content-digest");
        if (string.IsNullOrEmpty(headerValue))
            return false;

        // RFC 9530 digests the content octets as sent. Hashing a decoded string instead would let
        // charset decoding, BOM stripping and invalid UTF-8 replacement change what is verified, and
        // breaks against non-.NET senders whose body isn't plain UTF-8.
        var bodyBytes = await request.Content.ReadAsByteArrayAsync().ConfigureAwait(false);

        var recognised = 0;

        foreach (
            var entry in headerValue.Split(',', StringSplitOptions.RemoveEmptyEntries)
        )
        {
            var trimmed = entry.Trim();

            // The first '=' separates algorithm from value; base64 padding contributes later ones.
            var separator = trimmed.IndexOf('=');
            if (separator <= 0)
                continue;

            var algorithm = trimmed[..separator].Trim().ToLowerInvariant();
            var encoded = trimmed[(separator + 1)..].Trim().Trim(':');

            byte[] expected;
            switch (algorithm)
            {
                case "sha-512":
                    expected = SHA512.HashData(bodyBytes);
                    break;
                case "sha-256":
                    expected = SHA256.HashData(bodyBytes);
                    break;
                default:
                    continue;
            }

            if (
                !string.Equals(
                    Convert.ToBase64String(expected),
                    encoded,
                    StringComparison.Ordinal
                )
            )
                return false;

            recognised++;
        }

        return recognised > 0;
    }
}

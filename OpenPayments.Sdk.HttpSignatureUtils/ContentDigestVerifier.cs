using System.Security.Cryptography;
using System.Text;

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

        if (!request.Content.Headers.TryGetValues("Content-Digest", out var values))
            return false;

        // Hashes the UTF-8 bytes of the decoded body to mirror HttpRequestSigner.ComputeContentDigest
        // exactly. Hashing raw octets would be closer to RFC 9530, but matching the signer is what
        // guarantees the round trip.
        var body = await request.Content.ReadAsStringAsync().ConfigureAwait(false);
        var bodyBytes = Encoding.UTF8.GetBytes(body);

        var recognised = 0;

        foreach (
            var entry in string.Join(", ", values).Split(',', StringSplitOptions.RemoveEmptyEntries)
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

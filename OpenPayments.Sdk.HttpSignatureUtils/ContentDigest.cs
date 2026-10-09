using System.Security.Cryptography;
using System.Text;

namespace OpenPayments.Sdk.HttpSignatureUtils;

/// <summary>
/// Open Payments Content-Digest helpers (sha-512, RFC 9530 structured field).
/// </summary>
internal static class ContentDigest
{
    public static string ForBody(ReadOnlySpan<byte> body)
    {
        var hash = SHA512.HashData(body);
        return $"sha-512=:{Convert.ToBase64String(hash)}:";
    }

    public static string ForBody(string body) => ForBody(Encoding.UTF8.GetBytes(body));

    public static async Task<bool> MatchesRequestAsync(HttpRequestMessage request)
    {
        if (request.Content is null)
            return false;

        // Same order as SignatureInputBuilder: request headers, then content headers.
        // A content header that matches a swapped body must not win over the signed value.
        string? digestHeader = null;
        if (request.Headers.TryGetValues("Content-Digest", out var values))
            digestHeader = values.FirstOrDefault();
        else if (request.Content.Headers.TryGetValues("Content-Digest", out var contentValues))
            digestHeader = contentValues.FirstOrDefault();

        if (string.IsNullOrEmpty(digestHeader))
            return false;

        var body = await request.Content.ReadAsByteArrayAsync();
        var expected = ForBody(body);
        return string.Equals(expected, digestHeader, StringComparison.Ordinal);
    }
}

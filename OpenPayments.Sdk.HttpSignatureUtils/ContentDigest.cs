using System.Security.Cryptography;
using System.Text;

namespace OpenPayments.Sdk.HttpSignatureUtils;

/// <summary>
/// Open Payments Content-Digest helpers (sha-512, RFC 9530 structured field).
/// </summary>
internal static class ContentDigest
{
    public static string ForBody(string body)
    {
        var hash = SHA512.HashData(Encoding.UTF8.GetBytes(body));
        return $"sha-512=:{Convert.ToBase64String(hash)}:";
    }

    public static async Task<bool> MatchesRequestAsync(HttpRequestMessage request)
    {
        if (request.Content is null)
            return false;

        string? digestHeader = null;
        if (request.Content.Headers.TryGetValues("Content-Digest", out var contentValues))
            digestHeader = contentValues.FirstOrDefault();
        else if (request.Headers.TryGetValues("Content-Digest", out var values))
            digestHeader = values.FirstOrDefault();

        if (string.IsNullOrEmpty(digestHeader))
            return false;

        var body = await request.Content.ReadAsStringAsync();
        var expected = ForBody(body);
        return string.Equals(expected, digestHeader, StringComparison.OrdinalIgnoreCase);
    }
}

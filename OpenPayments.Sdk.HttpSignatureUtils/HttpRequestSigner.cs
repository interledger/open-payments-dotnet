using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using NSec.Cryptography;
using OpenPayments.Sdk.HttpSignatureUtils;

[assembly: InternalsVisibleTo("OpenPayments.Sdk.HttpSignatureUtils.Tests")]

/// <summary>
/// Signature headers returned by the HttpRequestSigner.
/// </summary>
public class SignatureHeaders
{
    /// <summary>
    /// Signature header value.
    /// </summary>
    public string Signature { get; set; } = string.Empty;

    /// <summary>
    /// Signature input header value.
    /// </summary>
    public string SignatureInput { get; set; } = string.Empty;
}

/// <summary>
/// Signs an HTTP request using the Ed25519 signature algorithm.
/// </summary>
public static class HttpRequestSigner
{
    private static string BuildSignatureParams(List<string> components, string keyId, long created)
    {
        var fields = string.Join(" ", components.Select(c => $"\"{c}\""));
        return $"({fields});created={created};keyid=\"{keyId}\";alg=\"ed25519\"";
    }

    private static string ComputeContentDigest(string body)
    {
        var hash = SHA512.HashData(Encoding.UTF8.GetBytes(body));
        return Convert.ToBase64String(hash);
    }

    /// <summary>
    /// Signs an HTTP request using the Ed25519 signature algorithm.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="privateKey"></param>
    /// <param name="keyId"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static async Task<SignatureHeaders> SignHttpRequestAsync(
        HttpRequestMessage request,
        Key privateKey,
        string keyId
    )
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(privateKey);
        if (string.IsNullOrWhiteSpace(keyId))
            throw new ArgumentException("KeyId cannot be empty.", nameof(keyId));

        var components = new List<string> { "@method", "@target-uri" };
        var headers = request.Headers.ToDictionary(
            h => h.Key.ToLowerInvariant(),
            h => string.Join(", ", h.Value)
        );

        if (headers.ContainsKey("authorization"))
        {
            components.Add("authorization");
        }

        string? body = null;

        if (request.Content != null)
        {
            body = await request.Content.ReadAsStringAsync().ConfigureAwait(false);
            if (!string.IsNullOrEmpty(body))
            {
                components.AddRange(["content-digest", "content-length", "content-type"]);

                var digest = ComputeContentDigest(body);

                request.Content.Headers.TryAddWithoutValidation(
                    "Content-Digest",
                    $"sha-512=:{digest}:"
                );

                if (!request.Content.Headers.Contains("Content-Length"))
                {
                    request.Content.Headers.TryAddWithoutValidation(
                        "Content-Length",
                        Encoding.UTF8.GetByteCount(body).ToString()
                    );
                }

                if (!request.Content.Headers.Contains("Content-Type"))
                {
                    request.Content.Headers.TryAddWithoutValidation(
                        "Content-Type",
                        "application/json"
                    );
                }
            }
        }

        var created = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        // Built once, then used for both the Signature-Input header and the signed base. Two copies
        // could drift, and a header that disagrees with the signed base is unverifiable.
        var signatureParams = BuildSignatureParams(components, keyId, created);
        var signatureBase = SignatureBaseBuilder.Build(components, signatureParams, request);
        var signatureBytes = SignatureAlgorithm.Ed25519.Sign(
            privateKey,
            Encoding.UTF8.GetBytes(signatureBase)
        );
        var base64Signature = Convert.ToBase64String(signatureBytes);

        return new SignatureHeaders
        {
            Signature = $"sig1=:{base64Signature}:",
            SignatureInput = $"sig1={signatureParams}",
        };
    }
}

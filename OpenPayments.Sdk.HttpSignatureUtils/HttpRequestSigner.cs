using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using NSec.Cryptography;

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
    private static string BuildSignatureInput(List<string> components, string keyId, long created)
    {
        var fields = string.Join(" ", components.Select(h => $"\"{h}\""));
        return $"({fields});created={created};keyid=\"{keyId}\";alg=\"ed25519\"";
    }

    private static string ComputeContentDigest(string body)
    {
        var hash = SHA512.HashData(Encoding.UTF8.GetBytes(body));
        return Convert.ToBase64String(hash);
    }

    private static async Task<string> TryGetHeaderValueAsync(HttpRequestMessage request, string name)
    {
        name = name.ToLowerInvariant();

        if (request.Headers.TryGetValues(name, out var values))
        {
            return string.Join(", ", values);
        }

        if (request.Content != null && request.Content.Headers.TryGetValues(name, out var contentValues))
        {
            return string.Join(", ", contentValues);
        }

        if (name == "content-digest" && request.Content != null)
        {
            var body = await request.Content.ReadAsStringAsync();
            return $"sha-512=:{ComputeContentDigest(body)}:";
        }

        return "";
    }

    private static async Task<string> BuildSignatureBaseAsync(HttpRequestMessage request, List<string> components,
        long created, string keyId)
    {
        var lines = new List<string>();

        foreach (var component in components)
        {
            switch (component)
            {
                case "@method":
                    lines.Add($"\"@method\": {request.Method.Method.ToUpper()}");
                    break;
                case "@target-uri":
                    lines.Add($"\"@target-uri\": {request.RequestUri}");
                    break;
                default:
                    var value = await TryGetHeaderValueAsync(request, component);
                    lines.Add($"\"{component.ToLower()}\": {value}");
                    break;
            }
        }

        var fieldList = string.Join(" ", components.Select(c => $"\"{c}\""));
        lines.Add($"\"@signature-params\": ({fieldList});created={created};keyid=\"{keyId}\";alg=\"ed25519\"");

        return string.Join("\n", lines);
    }

    /// <summary>
    /// Signs an HTTP request using the Ed25519 signature algorithm.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="privateKey"></param>
    /// <param name="keyId"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static async Task<SignatureHeaders> SignHttpRequestAsync(HttpRequestMessage request, Key privateKey,
        string keyId)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(privateKey);
        if (string.IsNullOrWhiteSpace(keyId)) throw new ArgumentException("KeyId cannot be empty.", nameof(keyId));

        var components = new List<string> { "@method", "@target-uri" };
        var headers = request.Headers.ToDictionary(h => h.Key.ToLowerInvariant(), h => string.Join(", ", h.Value));

        if (headers.ContainsKey("authorization"))
        {
            components.Add("authorization");
        }

        if (request.Content != null)
        {
            var content = await request.Content.ReadAsStringAsync();
            if (!string.IsNullOrEmpty(content))
            {
                components.AddRange(["content-digest", "content-length", "content-type"]);

                var digest = ComputeContentDigest(content);

                request.Content.Headers.TryAddWithoutValidation("Content-Digest", $"sha-512=:{digest}:");

                if (!request.Content.Headers.Contains("Content-Length"))
                {
                    request.Content.Headers.TryAddWithoutValidation("Content-Length",
                        Encoding.UTF8.GetByteCount(content).ToString());
                }

                if (!request.Content.Headers.Contains("Content-Type"))
                {
                    request.Content.Headers.TryAddWithoutValidation("Content-Type", "application/json");
                }
            }
        }

        var created = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var signatureInput = BuildSignatureInput(components, keyId, created);
        var signatureBase = await BuildSignatureBaseAsync(request, components, created, keyId);
        var signatureBytes = SignatureAlgorithm.Ed25519.Sign(privateKey, Encoding.UTF8.GetBytes(signatureBase));
        var base64Signature = Convert.ToBase64String(signatureBytes);

        return new SignatureHeaders
        {
            Signature = $"sig1=:{base64Signature}:",
            SignatureInput = $"sig1={signatureInput}"
        };
    }
}

using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using NSec.Cryptography;

[assembly: InternalsVisibleTo("OpenPayments.Sdk.HttpSignatureUtils.Tests")]

internal class SignatureHeaders
{
    public string Signature { get; set; } = string.Empty;
    public string SignatureInput { get; set; } = string.Empty;
}

internal static class HttpRequestSigner
{
    private static string BuildSignatureInput(List<string> components, string keyId, long created)
    {
        string fields = string.Join(" ", components.Select(h => $"\"{h}\""));
        return $"({fields});created={created};keyid=\"{keyId}\"";
    }

    private static string ComputeContentDigest(string body)
    {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(body));
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
            string body = await request.Content.ReadAsStringAsync();
            return $"sha-256=:{ComputeContentDigest(body)}:";
        }

        return "";
    }

    private static async Task<string> BuildSignatureBaseAsync(HttpRequestMessage request, List<string> components, long created, string keyId)
    {
        var lines = new List<string>();

        foreach (var component in components)
        {
            switch (component)
            {
                case "@method":
                    lines.Add($"@method: {request.Method.Method.ToLower()}");
                    break;
                case "@target-uri":
                    lines.Add($"@target-uri: {request.RequestUri}");
                    break;
                default:
                    string value = await TryGetHeaderValueAsync(request, component);
                    lines.Add($"{component.ToLower()}: {value}");
                    break;
            }
        }

        string fieldList = string.Join(" ", components.Select(c => $"\"{c}\""));
        lines.Add($"\"@signature-params\": ({fieldList});created={created};keyid=\"{keyId}\"");

        return string.Join("\n", lines);
    }

    public static async Task<SignatureHeaders> SignHttpRequestAsync(HttpRequestMessage request, Key privateKey, string keyId)
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
            string content = await request.Content.ReadAsStringAsync();
            if (!string.IsNullOrEmpty(content))
            {
                components.AddRange(["content-digest", "content-length", "content-type"]);

                string digest = ComputeContentDigest(content);

                request.Content.Headers.TryAddWithoutValidation("Content-Digest", $"sha-256=:{digest}:");
                request.Content.Headers.TryAddWithoutValidation("Content-Length", Encoding.UTF8.GetByteCount(content).ToString());

                if (!request.Content.Headers.Contains("Content-Type"))
                {
                    request.Content.Headers.TryAddWithoutValidation("Content-Type", "application/json");
                }
            }
        }

        long created = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string signatureInput = BuildSignatureInput(components, keyId, created);
        string signatureBase = await BuildSignatureBaseAsync(request, components, created, keyId);

        byte[] signatureBytes = SignatureAlgorithm.Ed25519.Sign(privateKey, Encoding.UTF8.GetBytes(signatureBase));
        string base64Signature = Convert.ToBase64String(signatureBytes);

        return new SignatureHeaders
        {
            Signature = $"sig1=:{base64Signature}:",
            SignatureInput = $"sig1={signatureInput}"
        };
    }
}
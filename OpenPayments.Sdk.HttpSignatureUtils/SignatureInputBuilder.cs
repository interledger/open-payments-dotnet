using System.Security.Cryptography;
using System.Text;

namespace Interledger.OpenPayments.HttpSignatureUtils;

/// <inheritdoc cref="ISignatureInputBuilder"/>
public class SignatureInputBuilder : ISignatureInputBuilder
{
    /// <inheritdoc cref="ISignatureInputBuilder"/>
    public async Task<string?> BuildBaseAsync(
        List<string> components,
        HttpRequestMessage request,
        string sigInput
    )
    {
        var sb = new StringBuilder();

        foreach (var component in components)
        {
            switch (component)
            {
                case "@method":
                    // RFC 9421 §2.2.1: uppercase, and HttpRequestSigner signs it uppercase —
                    // the base built here must be byte-identical to the one that was signed.
                    sb.Append($"\"@method\": {request.Method.Method.ToUpperInvariant()}\n");
                    break;
                case "@target-uri":
                    sb.Append($"\"@target-uri\": {request.RequestUri}\n");
                    break;
                default:
                    var value = await GetHeaderValueAsync(request, component);
                    sb.Append($"\"{component.ToLowerInvariant()}\": {value}\n");
                    break;
            }
        }

        sb.Append($"\"@signature-params\": {sigInput.Replace("sig1=", "")}");
        return sb.ToString();
    }

    private static async Task<string> GetHeaderValueAsync(HttpRequestMessage request, string name)
    {
        if (request.Headers.TryGetValues(name, out var values))
            return string.Join(", ", values);
        if (request.Content?.Headers.TryGetValues(name, out var cvalues) == true)
            return string.Join(", ", cvalues);

        if (name == "content-digest" && request.Content != null)
        {
            // sha-512, matching HttpRequestSigner's ComputeContentDigest.
            var body = await request.Content.ReadAsStringAsync();
            var hash = SHA512.HashData(Encoding.UTF8.GetBytes(body));
            return $"sha-512=:{Convert.ToBase64String(hash)}:";
        }

        return "";
    }
}

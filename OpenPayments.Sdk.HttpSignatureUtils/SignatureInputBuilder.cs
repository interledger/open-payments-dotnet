using System.Text;
using OpenPayments.Sdk.HttpSignatureUtils;

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
                    // RFC 9421: @method is case-normalized to uppercase (matches HttpRequestSigner).
                    sb.AppendLine($"\"@method\": {request.Method.Method.ToUpperInvariant()}");
                    break;
                case "@target-uri":
                    sb.AppendLine($"\"@target-uri\": {request.RequestUri}");
                    break;
                default:
                    var value = await GetHeaderValueAsync(request, component);
                    sb.AppendLine($"\"{component}\": {value}");
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
            var body = await request.Content.ReadAsStringAsync();
            return ContentDigest.ForBody(body);
        }

        return "";
    }
}

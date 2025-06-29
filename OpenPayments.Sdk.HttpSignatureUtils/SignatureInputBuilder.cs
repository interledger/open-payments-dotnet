using System.Security.Cryptography;
using System.Text;

internal class SignatureInputBuilder : ISignatureInputBuilder
{
    public async Task<string?> BuildBaseAsync(List<string> components, HttpRequestMessage request, string sigInput)
    {
        var sb = new StringBuilder();

        foreach (var component in components)
        {
            switch (component)
            {
                case "@method":
                    sb.AppendLine($"\"@method\": {request.Method.Method.ToLower()}");
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
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(body));
            return $"sha-256=:{Convert.ToBase64String(hash)}:";
        }

        return "";
    }
}
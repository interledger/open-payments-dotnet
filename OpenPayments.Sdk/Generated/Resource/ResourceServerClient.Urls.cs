namespace OpenPayments.Sdk.Generated.Resource;

public partial class ResourceServerClient
{
    /// <summary>
    /// Joins <paramref name="segment"/> onto <paramref name="baseUrl"/> with exactly one
    /// separating slash, whether or not the caller's URL carries a trailing one.
    /// </summary>
    private static string AppendPath(Uri baseUrl, string segment) =>
        $"{baseUrl.ToString().TrimEnd('/')}/{segment}";
}

/// <summary>
/// Defines the contract for building a signature input string based on given components, an HTTP request,
/// and a provided signature input string.
/// </summary>
public interface ISignatureInputBuilder
{
    /// <summary>
    /// Builds the base signature input string.
    /// </summary>
    /// <param name="components"></param>
    /// <param name="request"></param>
    /// <param name="sigInput"></param>
    /// <returns></returns>
    Task<string?> BuildBaseAsync(
        List<string> components,
        HttpRequestMessage request,
        string sigInput
    );
}

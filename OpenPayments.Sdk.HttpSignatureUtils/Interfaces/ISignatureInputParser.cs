/// <summary>
/// Defines the contract for parsing a signature input string into a list of components.
/// </summary>
public interface ISignatureInputParser
{
    /// <summary>
    /// Parses a signature input string into a list of components.
    /// </summary>
    /// <param name="sigInput"></param>
    /// <returns></returns>
    List<string>? GetComponents(string sigInput);
}

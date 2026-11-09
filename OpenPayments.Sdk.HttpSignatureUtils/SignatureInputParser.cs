/// <inheritdoc cref="ISignatureInputParser"/>
public class SignatureInputParser : ISignatureInputParser
{
    private const string Label = "sig1=";

    /// <inheritdoc cref="ISignatureInputParser"/>
    public List<string>? GetComponents(string sigInput)
    {
        if (string.IsNullOrEmpty(sigInput))
            return null;

        var labelIndex = sigInput.IndexOf(Label, StringComparison.Ordinal);
        if (labelIndex < 0)
            return null;

        var inputPart = sigInput[(labelIndex + Label.Length)..].Split(';')[0];

        var components = inputPart
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(c => c.Trim('"', '(', ')'))
            .Where(c => c.Length > 0)
            .ToList();

        // A signature covering nothing is not something to hand downstream as valid input.
        return components.Count == 0 ? null : components;
    }
}

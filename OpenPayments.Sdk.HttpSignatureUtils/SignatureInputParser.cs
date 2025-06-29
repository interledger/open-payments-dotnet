internal class SignatureInputParser : ISignatureInputParser
{
    public List<string>? GetComponents(string sigInput)
    {
        var inputPart = sigInput.Split("sig1=")[1].Split(';')[0];
        return inputPart
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(c => c.Trim('"', '(', ')'))
            .ToList();
    }
}
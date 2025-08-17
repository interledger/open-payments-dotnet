public interface ISignatureInputParser
{
    List<string>? GetComponents(string sigInput);
}
internal interface ISignatureInputBuilder
{
    Task<string?> BuildBaseAsync(List<string> components, HttpRequestMessage request, string sigInput);
}
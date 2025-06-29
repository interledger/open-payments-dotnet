internal interface ISignatureInputValidator
{
    bool Validate(List<string> components, HttpRequestMessage request);
}
using OpenPayments.Sdk.HttpSignatureUtils;

public interface IHttpSignatureValidator
{
    Task<bool> ValidateSignatureAsync(HttpRequestMessage request, Jwk clientKey);
    bool AreSignatureHeadersPresent(HttpRequestMessage request);
}
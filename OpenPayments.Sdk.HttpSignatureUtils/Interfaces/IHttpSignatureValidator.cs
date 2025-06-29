using OpenPayments.Sdk.HttpSignatureUtils;

internal interface IHttpSignatureValidator
{
    Task<bool> ValidateSignatureAsync(HttpRequestMessage request, Jwk clientKey);
    bool AreSignatureHeadersPresent(HttpRequestMessage request);
}
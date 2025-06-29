using OpenPayments.Sdk.HttpSignatureUtils;

public static class HttpClientSignatureExtensions
{
    // public static async Task<bool> ValidateRequestSignatureAsync(
    //     this HttpClient client,
    //     HttpRequestMessage request,
    //     Jwk clientKey)
    // {
    //     if (!HttpSignatureValidator.AreSignatureHeadersPresent(request))
    //         return false;

    //     return await HttpSignatureValidator.ValidateSignatureAsync(request, clientKey);
    // }
}
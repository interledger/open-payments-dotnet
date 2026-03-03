using OpenPayments.Sdk.HttpSignatureUtils;

/// <summary>
/// Provides extension methods for adding signature-based authentication
/// to HTTP client requests.
/// </summary>
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

using OpenPayments.Sdk.HttpSignatureUtils;

/// <summary>
/// Provides extension methods for configuring and adding signature-based authentication
/// to instances of <see cref="System.Net.Http.HttpClient"/>.
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

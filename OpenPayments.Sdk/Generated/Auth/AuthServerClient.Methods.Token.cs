using System.Net.Http.Headers;
using OpenPayments.Sdk.Http;

namespace OpenPayments.Sdk.Generated.Auth;

public partial class AuthServerClient
{
    /// <param name="tokenUrl">Token Url for rotation.</param>
    /// <param name="accessToken">Access Token for rotation.</param>
    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <summary>
    /// Rotate Access Token
    /// </summary>
    /// <remarks>
    /// Management endpoint to rotate access token.
    /// </remarks>
    /// <returns>OK</returns>
    /// <exception cref="OpenPayments.Sdk.Exceptions.OpenPaymentsApiException">The request failed.</exception>
    public async Task<RotateTokenResponse> RotateTokenAsync(
        Uri tokenUrl,
        string accessToken,
        CancellationToken cancellationToken
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenUrl.ToString());
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        var client = _httpClient;
        using var request = new HttpRequestMessage();
        request.Content = new StringContent(
            string.Empty,
            System.Text.Encoding.UTF8,
            "application/json"
        );
        request.Method = new HttpMethod("POST");
        request.Headers.Authorization = new AuthenticationHeaderValue("GNAP", $"{accessToken}");
        request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json"));
        var urlBuilder = new System.Text.StringBuilder(tokenUrl.ToString());

        PrepareRequest(client, request, urlBuilder);

        var url = urlBuilder.ToString();
        request.RequestUri = new Uri(url, UriKind.RelativeOrAbsolute);

        PrepareRequest(client, request, url);

        var response = await client
            .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);

        try
        {
            await OpenPaymentsResponse
                .ThrowIfErrorAsync(response, cancellationToken)
                .ConfigureAwait(false);

            return await OpenPaymentsResponse
                .ReadRequiredAsync<RotateTokenResponse>(
                    response,
                    JsonSerializerSettings,
                    cancellationToken
                )
                .ConfigureAwait(false);
        }
        finally
        {
            response.Dispose();
        }
    }

    /// <param name="tokenUrl">Token Url for revocation.</param>
    /// <param name="accessToken">Access Token for revocation.</param>
    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <summary>
    /// Revoke Access Token
    /// </summary>
    /// <remarks>
    /// Management endpoint to revoke access token.
    /// </remarks>
    /// <returns>No Content</returns>
    /// <exception cref="OpenPayments.Sdk.Exceptions.OpenPaymentsApiException">The request failed.</exception>
    public async Task RevokeTokenAsync(
        Uri tokenUrl,
        string accessToken,
        CancellationToken cancellationToken
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenUrl.ToString(), nameof(tokenUrl));
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        var client = _httpClient;
        using var request = new HttpRequestMessage();
        request.Method = new HttpMethod("DELETE");
        request.Headers.Authorization = new AuthenticationHeaderValue("GNAP", $"{accessToken}");
        var urlBuilder = new System.Text.StringBuilder(tokenUrl.ToString());

        PrepareRequest(client, request, urlBuilder);

        var url = urlBuilder.ToString();
        request.RequestUri = new Uri(url, UriKind.RelativeOrAbsolute);
        PrepareRequest(client, request, url);

        var response = await client
            .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);

        try
        {
            await OpenPaymentsResponse
                .ThrowIfErrorAsync(response, cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            response.Dispose();
        }
    }
}

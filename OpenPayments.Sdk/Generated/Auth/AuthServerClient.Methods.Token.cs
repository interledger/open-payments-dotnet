using System.Net.Http.Headers;

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
    /// <exception cref="ApiException">A server side error occurred.</exception>
    public async Task<RotateTokenResponse> RotateTokenAsync(Uri tokenUrl, string accessToken, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenUrl.ToString());
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        var client = _httpClient;
        using var request = new HttpRequestMessage();
        request.Content = new StringContent(string.Empty, System.Text.Encoding.UTF8, "application/json");
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
            var headers = Helpers.ExtractHeaders(response);

            ProcessResponse(client, response);

            var status = (int)response.StatusCode;
            switch (status)
            {
                case 200:
                {
                    var objectResponse =
                        await ReadObjectResponseAsync<RotateTokenResponse>(response, headers, cancellationToken)
                            .ConfigureAwait(false);
                    if (objectResponse.Object == null)
                    {
                        throw new ApiException("Response was null which was not expected.", status,
                            objectResponse.Text, headers, null);
                    }

                    return objectResponse.Object;
                }
                case 400:
                case 401:
                case 404:
                case 500:
                {
                    var objectResponse =
                        await ReadObjectResponseAsync<ErrorResponse>(response, headers, cancellationToken)
                            .ConfigureAwait(false);
                    if (objectResponse.Object == null)
                    {
                        throw new ApiException("Response was null which was not expected.", status,
                            objectResponse.Text, headers, null);
                    }

                    throw new ApiException<ErrorResponse>(Helpers.StatusAsText(status), status,
                        objectResponse.Text,
                        headers, objectResponse.Object, null);
                }
                default:
                {
                    var responseData =
                        await ReadAsStringAsync(response.Content, cancellationToken).ConfigureAwait(false);
                    throw new ApiException(
                        "The HTTP status code of the response was not expected (" + status + ").", status,
                        responseData, headers, null);
                }
            }
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
    /// <exception cref="ApiException">A server side error occurred.</exception>
    public async Task RevokeTokenAsync(Uri tokenUrl, string accessToken, CancellationToken cancellationToken)
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
            var headers = Helpers.ExtractHeaders(response);

            ProcessResponse(client, response);

            var status = (int)response.StatusCode;
            switch (status)
            {
                case 204:
                    return;
                case 401:
                case 500:
                {
                    var objectResponse =
                        await ReadObjectResponseAsync<ErrorResponse>(response, headers, cancellationToken)
                            .ConfigureAwait(false);
                    if (objectResponse.Object == null)
                    {
                        throw new ApiException("Response was null which was not expected.", status,
                            objectResponse.Text, headers, null);
                    }

                    throw new ApiException<ErrorResponse>(Helpers.StatusAsText(status), status,
                        objectResponse.Text, headers, objectResponse.Object, null);
                }
                default:
                {
                    var responseData = await ReadAsStringAsync(response.Content, cancellationToken).ConfigureAwait(false);
                    throw new ApiException(
                        "The HTTP status code of the response was not expected (" + status + ").", status,
                        responseData, headers, null);
                }
            }
        }
        finally
        {
            response.Dispose();
        }
    }
}
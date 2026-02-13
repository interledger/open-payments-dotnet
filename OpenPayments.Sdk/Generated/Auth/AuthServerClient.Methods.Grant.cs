using System.Net.Http.Headers;
using Newtonsoft.Json;

namespace OpenPayments.Sdk.Generated.Auth;

public partial class AuthServerClient
{
    /// <param name="body">Body for grant request.</param>
    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <summary>
    /// Grant Request
    /// </summary>
    /// <remarks>
    /// Make a new grant request
    /// </remarks>
    /// <returns>OK</returns>
    /// <exception cref="ErrorResponse">A server side error occurred.</exception>
    public async Task<AuthResponse> CreateGrantAsync(GrantCreateBody body,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(body);

        var client = _httpClient;
        using var request = new HttpRequestMessage();
        var json = JsonConvert.SerializeObject(body, JsonSerializerSettings);
        var content = new StringContent(json);
        content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
        request.Content = content;
        request.Method = new HttpMethod("POST");
        request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json"));
        var urlBuilder = new System.Text.StringBuilder();
        if (!string.IsNullOrEmpty(_baseUrl)) urlBuilder.Append(_baseUrl);

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
                        await ReadObjectResponseAsync<AuthResponse>(response, headers, cancellationToken)
                            .ConfigureAwait(false);
                    if (objectResponse.Object == null)
                    {
                        throw new ApiException("Response was null which was not expected.", status,
                            objectResponse.Text, headers, null);
                    }

                    return objectResponse.Object;
                }
                case 400 or 401 or 500:
                {
                    var objectResponse =
                        await ReadObjectResponseAsync<ErrorResponse>(response, headers,
                            cancellationToken).ConfigureAwait(false);
                    if (objectResponse.Object == null)
                    {
                        throw new ApiException("Response was null which was not expected.", status,
                            objectResponse.Text, headers, null);
                    }

                    throw new ApiException<ErrorResponse>(objectResponse.Object.Error.Description, status,
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

    /// <param name="continueUrl">Url for grant continuation.</param>
    /// <param name="accessToken">Access Token for continuation.</param>
    /// <param name="body">Body for continuation.</param>
    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <summary>
    /// Continuation Request
    /// </summary>
    /// <remarks>
    /// Continue a grant request during or after user interaction.
    /// </remarks>
    /// <returns>Success</returns>
    /// <exception cref="ErrorResponse">A server side error occurred.</exception>
    public async Task<AuthResponse> ContinueGrantAsync(Uri continueUrl,
        string accessToken, GrantContinueBody body, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(continueUrl.ToString());
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        var client = _httpClient;
        using var request = new HttpRequestMessage();
        var json = JsonConvert.SerializeObject(body, JsonSerializerSettings);
        var content = new StringContent(json);
        content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
        request.Content = content;
        request.Method = new HttpMethod("POST");
        request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json"));
        request.Headers.Authorization = new AuthenticationHeaderValue("GNAP", $"{accessToken}");

        var urlBuilder = new System.Text.StringBuilder(continueUrl.ToString());

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
                        await ReadObjectResponseAsync<AuthResponse>(response, headers, cancellationToken)
                            .ConfigureAwait(false);
                    if (objectResponse.Object == null)
                    {
                        throw new ApiException("Response was null which was not expected.", status,
                            objectResponse.Text, headers, null);
                    }

                    return objectResponse.Object;
                }
                case 400 or 401 or 404:
                {
                    var objectResponse =
                        await ReadObjectResponseAsync<ErrorResponse>(response, headers,
                            cancellationToken).ConfigureAwait(false);
                    if (objectResponse.Object == null)
                    {
                        throw new ApiException("Response was null which was not expected.", status,
                            objectResponse.Text, headers, null);
                    }

                    throw new ApiException<ErrorResponse>(objectResponse.Object.Error.Description, status,
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

    /// <param name="continueUrl">Continue URL.</param>
    /// <param name="accessToken">Access Token.</param>
    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <summary>
    /// Cancel Grant
    /// </summary>
    /// <remarks>
    /// Cancel a grant request or delete a grant client side.
    /// </remarks>
    /// <returns>No Content</returns>
    /// <exception cref="ErrorResponse">A server side error occurred.</exception>
    public async Task CancelGrantAsync(
        Uri continueUrl, string accessToken,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(continueUrl.ToString());
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        var client = _httpClient;

        using var request = new HttpRequestMessage();
        request.Method = new HttpMethod("DELETE");
        request.Headers.Authorization = new AuthenticationHeaderValue("GNAP", $"{accessToken}");

        var urlBuilder = new System.Text.StringBuilder(continueUrl.ToString());

        PrepareRequest(client, request, urlBuilder);

        var url = urlBuilder.ToString();
        request.RequestUri = new Uri(url, UriKind.RelativeOrAbsolute);

        PrepareRequest(client, request, url);

        var response = await client.SendAsync(request,
                HttpCompletionOption.ResponseHeadersRead, cancellationToken)
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
                case 401 or 404:
                {
                    var objectResponse =
                        await ReadObjectResponseAsync<ErrorResponse>(response, headers,
                            cancellationToken).ConfigureAwait(false);
                    if (objectResponse.Object == null)
                    {
                        throw new ApiException("Response was null which was not expected.", status,
                            objectResponse.Text, headers, null);
                    }

                    throw new ApiException<ErrorResponse>(objectResponse.Object.Error.Description, status,
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
}
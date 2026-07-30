using Newtonsoft.Json;
using OpenPayments.Sdk.Generated.Auth;

namespace OpenPayments.Sdk.Http;

public class AuthServerClient : OpenPaymentsClientBase
{
    public AuthServerClient(HttpClient httpClient)
        : base(httpClient, new JsonSerializerSettings { ContractResolver = new AuthContractResolver() })
    {
    }

    /// <param name="authServerUrl">Auth server URL.</param>
    /// <param name="body">Body for grant request.</param>
    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <summary>
    /// Grant Request
    /// </summary>
    /// <remarks>
    /// Make a new grant request
    /// </remarks>
    /// <returns>OK</returns>
    /// <exception cref="OpenPayments.Sdk.Exceptions.OpenPaymentsApiException">The request failed.</exception>
    public async Task<AuthResponse> CreateGrantAsync(
        Uri authServerUrl,
        GrantCreateBody body,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(body);

        return await SendAsync<AuthResponse>(
                HttpMethod.Post, authServerUrl, body, accessToken: null, cancellationToken)
            .ConfigureAwait(false);
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
    /// <exception cref="OpenPayments.Sdk.Exceptions.OpenPaymentsApiException">The request failed.</exception>
    public async Task<AuthResponse> ContinueGrantAsync(
        Uri continueUrl,
        string accessToken,
        GrantContinueBody body,
        CancellationToken cancellationToken
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(continueUrl.ToString());
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        return await SendAsync<AuthResponse>(
                HttpMethod.Post, continueUrl, body, accessToken, cancellationToken)
            .ConfigureAwait(false);
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
    /// <exception cref="OpenPayments.Sdk.Exceptions.OpenPaymentsApiException">The request failed.</exception>
    public async Task CancelGrantAsync(
        Uri continueUrl,
        string accessToken,
        CancellationToken cancellationToken
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(continueUrl.ToString());
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        await SendAsync(HttpMethod.Delete, continueUrl, body: null, accessToken, cancellationToken)
            .ConfigureAwait(false);
    }

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

        return await SendAsync<RotateTokenResponse>(
                HttpMethod.Post, tokenUrl, body: null, accessToken, cancellationToken)
            .ConfigureAwait(false);
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

        await SendAsync(HttpMethod.Delete, tokenUrl, body: null, accessToken, cancellationToken)
            .ConfigureAwait(false);
    }
}

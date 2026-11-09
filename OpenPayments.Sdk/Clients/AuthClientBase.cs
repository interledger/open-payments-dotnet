using OpenPayments.Sdk.Generated.Auth;
using OpenPayments.Sdk.Http;

namespace OpenPayments.Sdk.Clients;

/// <summary>
/// Default <see cref="IAuthClientBase"/> implementation. Wraps an <see cref="AuthServerClient"/>, passing
/// the target URL from <see cref="RequestArgs.Url"/> on each call since a single auth server client is
/// shared across requests to different authorization servers.
/// </summary>
internal sealed class AuthClientBase : IAuthClientBase
{
    private readonly AuthServerClient _client;
    private readonly Uri _clientUrl;

    /// <summary>
    /// Creates a new <see cref="AuthClientBase"/>.
    /// </summary>
    /// <param name="http"><see cref="HttpClient"/> whose pipeline signs outgoing requests. Its <see cref="HttpClient.BaseAddress"/> is ignored; absolute request URIs are used instead.</param>
    /// <param name="clientUrl">Client wallet address URL (e.g. <c>https://wallet.example/alice</c>), sent as the <c>client</c> field of grant requests.</param>
    public AuthClientBase(HttpClient http, Uri clientUrl)
    {
        ArgumentNullException.ThrowIfNull(clientUrl);

        _client = new AuthServerClient(http);
        _clientUrl = clientUrl;
    }

    /// <inheritdoc/>
    public async Task<AuthResponse> RequestGrantAsync(
        RequestArgs requestArgs,
        GrantCreateBody body,
        CancellationToken cancellationToken = default
    )
    {
        body.Client = _clientUrl;

        return await _client
            .CreateGrantAsync(requestArgs.Url, body, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<AuthResponse> ContinueGrantAsync(
        AuthRequestArgs requestArgs,
        GrantContinueBody body,
        CancellationToken cancellationToken = default
    )
    {
        return await _client
            .ContinueGrantAsync(requestArgs.Url, requestArgs.AccessToken, body, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task CancelGrantAsync(
        AuthRequestArgs requestArgs,
        CancellationToken cancellationToken
    )
    {
        await _client
            .CancelGrantAsync(requestArgs.Url, requestArgs.AccessToken, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<RotateTokenResponse> RotateTokenAsync(
        AuthRequestArgs requestArgs,
        CancellationToken cancellationToken
    )
    {
        return await _client
            .RotateTokenAsync(requestArgs.Url, requestArgs.AccessToken!, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task RevokeTokenAsync(
        AuthRequestArgs requestArgs,
        CancellationToken cancellationToken = default
    )
    {
        await _client
            .RevokeTokenAsync(requestArgs.Url, requestArgs.AccessToken!, cancellationToken)
            .ConfigureAwait(false);
    }
}

/// <summary>
/// Authorization-server operations (grant request, continuation, cancellation, and token
/// rotation/revocation) available to an authenticated Open Payments client.
/// </summary>
internal interface IAuthClientBase
{
    /// <summary>
    /// Requests a new access grant from the authorization server.
    /// </summary>
    /// <param name="requestArgs">The authorization server URL to send the request to.</param>
    /// <param name="body">The grant request body, describing the access being requested.</param>
    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <returns>The grant response — either a completed grant, or one requiring interaction/continuation.</returns>
    public Task<AuthResponse> RequestGrantAsync(
        RequestArgs requestArgs,
        GrantCreateBody body,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Continues a pending grant request after the client has finished any required interaction.
    /// </summary>
    /// <param name="requestArgs">The grant continuation URL and continuation access token to use for the request.</param>
    /// <param name="body">The continuation request body, including the interaction reference.</param>
    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <returns>The completed grant response.</returns>
    public Task<AuthResponse> ContinueGrantAsync(
        AuthRequestArgs requestArgs,
        GrantContinueBody body,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Cancels a pending or active grant.
    /// </summary>
    /// <param name="requestArgs">The grant URL and continuation access token to use for the request.</param>
    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    public Task CancelGrantAsync(AuthRequestArgs requestArgs, CancellationToken cancellationToken);

    /// <summary>
    /// Rotates an access token, exchanging it for a newly issued token with the same access rights.
    /// </summary>
    /// <param name="requestArgs">The token management URL and current access token to use for the request.</param>
    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <returns>The rotated access token.</returns>
    public Task<RotateTokenResponse> RotateTokenAsync(
        AuthRequestArgs requestArgs,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Revokes an access token, ending the grant it was issued under.
    /// </summary>
    /// <param name="requestArgs">The token management URL and access token to revoke.</param>
    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    public Task RevokeTokenAsync(
        AuthRequestArgs requestArgs,
        CancellationToken cancellationToken = default
    );
}

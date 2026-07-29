using System.Globalization;
using NSec.Cryptography;
using OpenPayments.Sdk.Generated.Auth;

namespace OpenPayments.Sdk.Clients;

/// <summary>
/// Default <see cref="IAuthClientBase"/> implementation. Wraps a signed <see cref="AuthServerClient"/>,
/// passing the target URL from <see cref="RequestArgs.Url"/> on each call (rewriting <c>BaseUrl</c> for
/// the initial grant request) since a single auth server client is shared across requests to different
/// authorization servers.
/// </summary>
public class AuthClientBase : IAuthClientBase
{
    private readonly AuthServerClient _client;
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Creates a new <see cref="AuthClientBase"/> that signs every outgoing request with the given key.
    /// </summary>
    /// <param name="http">Pre-configured <see cref="HttpClient"/> instance. Its <see cref="HttpClient.BaseAddress"/> is ignored; absolute request URIs are used instead.</param>
    /// <param name="privateKey">Private key used to sign requests.</param>
    /// <param name="keyId">Key ID used to sign requests.</param>
    /// <param name="clientUrl">Client wallet address URL (e.g. <c>https://wallet.example/alice</c>).</param>
    public AuthClientBase(HttpClient http, Key privateKey, string keyId, Uri clientUrl)
    {
        _httpClient = http;
        _client = new AuthServerClient(http);
        _client.AddSigningKey(privateKey, keyId);
        _client.ClientUrl = clientUrl;
    }

    /// <inheritdoc/>
    public async Task<AuthResponse> RequestGrantAsync(
        RequestArgs requestArgs,
        GrantCreateBody body,
        CancellationToken cancellationToken = default
    )
    {
        _client.BaseUrl = requestArgs.Url.ToString();
        body.Client = _client.ClientUrl;

        return await _client.CreateGrantAsync(body, cancellationToken).ConfigureAwait(false);
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
public interface IAuthClientBase
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

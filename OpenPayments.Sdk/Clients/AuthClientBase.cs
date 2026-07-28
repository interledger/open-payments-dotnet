using System.Globalization;
using OpenPayments.Sdk.Generated.Auth;

namespace OpenPayments.Sdk.Clients;

/// <summary>Default <see cref="IAuthClientBase"/> implementation over <see cref="AuthServerClient"/>.</summary>
public class AuthClientBase : IAuthClientBase
{
    private readonly AuthServerClient _client;

    /// <summary>Creates the client. Signing must already be configured on <paramref name="http"/>'s handler pipeline.</summary>
    /// <param name="http">The HTTP client used for all requests.</param>
    /// <param name="clientUrl">Client wallet address URL, sent as the <c>client</c> field of grant requests.</param>
    public AuthClientBase(HttpClient http, Uri clientUrl)
    {
        _client = new AuthServerClient(http);
        _client.ClientUrl = clientUrl;
    }

    /// <inheritdoc/>
    public async Task<AuthResponse> RequestGrantAsync(
        RequestArgs requestArgs,
        GrantCreateBody body,
        CancellationToken cancellationToken = default
    )
    {
        body.Client = _client.ClientUrl;

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
/// Low-level client for the Open Payments authorization server (GNAP): grant lifecycle
/// and access-token management. Wrapped by <see cref="IAuthenticatedClient"/>, which is
/// the surface most consumers should use.
/// </summary>
public interface IAuthClientBase
{
    /// <summary>Requests a new grant from the authorization server.</summary>
    /// <param name="requestArgs">Authorization server grant endpoint URL.</param>
    /// <param name="body">The grant request (requested access, client, optional interact).</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    public Task<AuthResponse> RequestGrantAsync(
        RequestArgs requestArgs,
        GrantCreateBody body,
        CancellationToken cancellationToken = default
    );

    /// <summary>Continues a pending (interactive) grant.</summary>
    /// <param name="requestArgs">Continue URI and continuation access token from the initial grant response.</param>
    /// <param name="body">The continuation request (interaction reference).</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    public Task<AuthResponse> ContinueGrantAsync(
        AuthRequestArgs requestArgs,
        GrantContinueBody body,
        CancellationToken cancellationToken = default
    );

    /// <summary>Cancels a grant, revoking any access it carries.</summary>
    /// <param name="requestArgs">Grant management URL and access token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task CancelGrantAsync(AuthRequestArgs requestArgs, CancellationToken cancellationToken);

    /// <summary>Rotates an access token, returning a newly issued replacement.</summary>
    /// <param name="requestArgs">Token management URL and current access token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task<RotateTokenResponse> RotateTokenAsync(
        AuthRequestArgs requestArgs,
        CancellationToken cancellationToken
    );

    /// <summary>Revokes an access token, rendering it invalid.</summary>
    /// <param name="requestArgs">Token management URL and access token.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    public Task RevokeTokenAsync(
        AuthRequestArgs requestArgs,
        CancellationToken cancellationToken = default
    );
}

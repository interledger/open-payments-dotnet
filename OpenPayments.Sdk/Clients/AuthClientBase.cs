using System.Globalization;
using NSec.Cryptography;
using OpenPayments.Sdk.Generated.Auth;

namespace OpenPayments.Sdk.Clients;

public class AuthClientBase : IAuthClientBase
{
    private readonly AuthServerClient _client;
    private readonly HttpClient _httpClient;

    public AuthClientBase(HttpClient http, Key privateKey, string keyId, Uri clientUrl)
    {
        _httpClient = http;
        _client = new AuthServerClient(http);
        _client.AddSigningKey(privateKey, keyId);
        _client.ClientUrl = clientUrl;
    }

    public async Task<AuthResponse> RequestGrantAsync(
        RequestArgs requestArgs,
        GrantCreateBody body,
        CancellationToken cancellationToken = default
    )
    {
        ValidateGrantRequest(body);

        _client.BaseUrl = requestArgs.Url.ToString();
        body.Client = _client.ClientUrl;

        return await _client.CreateGrantAsync(body, cancellationToken).ConfigureAwait(false);
    }

    private static void ValidateGrantRequest(GrantCreateBody body)
    {
        if (body.AccessToken == null && body.Subject == null)
        {
            throw new ArgumentException(
                "Grant request must include at least one of \"access_token\" or \"subject\"."
            );
        }

        var access = body.AccessToken?.Access;
        if (access == null)
        {
            return;
        }

        foreach (var item in access)
        {
            if (
                item.Type == AccessType.OutgoingPayment
                && item.Limits?.DebitAmount != null
                && item.Limits?.ReceiveAmount != null
            )
            {
                throw new ArgumentException(
                    "Grant request outgoing-payment limits cannot include both \"debitAmount\" and \"receiveAmount\"."
                );
            }
        }
    }

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

    public async Task CancelGrantAsync(
        AuthRequestArgs requestArgs,
        CancellationToken cancellationToken
    )
    {
        await _client
            .CancelGrantAsync(requestArgs.Url, requestArgs.AccessToken, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<RotateTokenResponse> RotateTokenAsync(
        AuthRequestArgs requestArgs,
        CancellationToken cancellationToken
    )
    {
        return await _client
            .RotateTokenAsync(requestArgs.Url, requestArgs.AccessToken!, cancellationToken)
            .ConfigureAwait(false);
    }

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

public interface IAuthClientBase
{
    public Task<AuthResponse> RequestGrantAsync(
        RequestArgs requestArgs,
        GrantCreateBody body,
        CancellationToken cancellationToken = default
    );

    public Task<AuthResponse> ContinueGrantAsync(
        AuthRequestArgs requestArgs,
        GrantContinueBody body,
        CancellationToken cancellationToken = default
    );

    public Task CancelGrantAsync(AuthRequestArgs requestArgs, CancellationToken cancellationToken);

    public Task<RotateTokenResponse> RotateTokenAsync(
        AuthRequestArgs requestArgs,
        CancellationToken cancellationToken
    );

    public Task RevokeTokenAsync(
        AuthRequestArgs requestArgs,
        CancellationToken cancellationToken = default
    );
}

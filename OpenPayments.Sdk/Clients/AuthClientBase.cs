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

    public async Task<AuthResponse> RequestGrantAsync(RequestArgs requestArgs,
        GrantCreateBody body,
        CancellationToken cancellationToken = default)
    {
        _client.BaseUrl = requestArgs.Url.ToString();
        body.Client = _client.ClientUrl;

        return await _client
            .CreateGrantAsync(
                body
                , cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<AuthResponse> ContinueGrantAsync(RequestArgs requestArgs,
        GrantContinueBody body,
        CancellationToken cancellationToken = default)
    {
        return await _client
            .ContinueGrantAsync(
                requestArgs.Url,
                requestArgs.AccessToken!,
                body,
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task CancelGrantAsync(RequestArgs requestArgs,
        CancellationToken cancellationToken)
    {
        await _client
            .CancelGrantAsync(
                requestArgs.Url,
                requestArgs.AccessToken!,
                cancellationToken)
            .ConfigureAwait(false);
    }
}

public interface IAuthClientBase
{
    public Task<AuthResponse> RequestGrantAsync(RequestArgs requestArgs,
        GrantCreateBody body,
        CancellationToken cancellationToken = default);

    public Task<AuthResponse> ContinueGrantAsync(RequestArgs requestArgs,
        GrantContinueBody body,
        CancellationToken cancellationToken = default);

    public Task CancelGrantAsync(RequestArgs requestArgs, CancellationToken cancellationToken);
}
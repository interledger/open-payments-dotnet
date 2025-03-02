namespace OpenPaymentsSDK.Interfaces;

/// <summary>
/// Represents the dependencies for an authenticated client.
/// </summary>
public interface IAuthenticatedClientDeps : IUnauthenticatedClientDeps
{
    /// <summary>
    /// An optional OpenAPI specification for the authorization server.
    /// </summary>
    Generated.AS.Client AuthServerOpenApi { get; }
}
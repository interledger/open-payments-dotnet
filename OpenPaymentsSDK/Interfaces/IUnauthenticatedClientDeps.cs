namespace OpenPaymentsSDK.Interfaces;

/// <summary>
/// Represents the dependencies for an unauthenticated client.
/// </summary>
public interface IUnauthenticatedClientDeps: IBaseDeps
{
    /// <summary>
    /// OpenAPI specification for the wallet address server.
    /// </summary>
    Generated.WA.Client WalletAddressServerOpenApi { get; }
    
    /// <summary>
    /// OpenAPI specification for the resource server.
    /// </summary>
    Generated.RS.Client ResourceServerOpenApi { get; }
}
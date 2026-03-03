using OpenPayments.Sdk.Generated.Auth;
using OpenPayments.Sdk.Generated.Resource;

namespace OpenPayments.Sdk.Clients;

/// <summary>
/// Represents a client used to interact with Open Payments endpoints
/// that require authentication.
/// </summary>
public interface IAuthenticatedClient : IUnauthenticatedClient
{
    /// <summary>
    /// Request Grant
    /// </summary>
    /// <param name="requestArgs">Grant URL (e.g. <c>https://ilp.com/grant</c>) and access token.</param>
    /// <param name="body">Request body</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    public Task<AuthResponse> RequestGrantAsync(
        RequestArgs requestArgs,
        GrantCreateBody body,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Continue Grant
    /// </summary>
    /// <param name="requestArgs">Continue Grant URL (e.g. <c>https://ilp.com/continue/1234</c>) and access token.</param>
    /// <param name="body"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<AuthResponse> ContinueGrantAsync(
        AuthRequestArgs requestArgs,
        GrantContinueBody? body = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Cancel Grant
    /// </summary>
    /// <param name="requestArgs">Cancel Grant URL (e.g. <c>https://ilp.com/grant/1234</c>) and access token.</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task CancelGrantAsync(
        AuthRequestArgs requestArgs,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Rotates an access token to retrieve a newly issued token based on the provided request arguments.
    /// </summary>
    /// <param name="requestArgs">Token URL and current access token required for token rotation.</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation.</param>
    /// <returns>A response containing the rotated token and additional metadata.</returns>
    public Task<RotateTokenResponse> RotateTokenAsync(
        AuthRequestArgs requestArgs,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Revokes an access token, rendering it invalid for future use.
    /// </summary>
    /// <param name="requestArgs">Auth Token URL Address (e.g. <c>https://ilp.com/token/1234</c>) and access token.</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task RevokeTokenAsync(
        AuthRequestArgs requestArgs,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Creates a new incoming payment.
    /// </summary>
    /// <param name="requestArgs">The resource server URL address (e.g. <c>https://res.ilp.com/incoming/</c>) and access token.</param>
    /// <param name="body">The details of the incoming payment request.</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation.</param>
    /// <returns>A response containing the details of the created incoming payment.</returns>
    public Task<IncomingPaymentResponse> CreateIncomingPaymentAsync(
        AuthRequestArgs requestArgs,
        IncomingPaymentBody body,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Get an Incoming Payment
    /// </summary>
    /// <param name="requestArgs"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>IncomingPaymentResponse</returns>
    public Task<IncomingPaymentResponse> GetIncomingPaymentAsync(
        AuthRequestArgs requestArgs,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Get a Public Incoming Payment
    /// </summary>
    /// <param name="requestArgs"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<PublicIncomingPayment> GetPublicIncomingPaymentAsync(
        RequestArgs requestArgs,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// List Incoming Payments
    /// </summary>
    /// <param name="requestArgs"></param>
    /// <param name="query"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>ListIncomingPaymentsResponse</returns>
    public Task<ListIncomingPaymentsResponse> ListIncomingPaymentsAsync(
        AuthRequestArgs requestArgs,
        ListIncomingPaymentQuery query,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Complete Incoming Payment
    /// </summary>
    /// <param name="requestArgs"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>ListIncomingPaymentsResponse</returns>
    public Task<IncomingPaymentResponse> CompleteIncomingPaymentsAsync(
        AuthRequestArgs requestArgs,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Creates a quote for a payment transaction.
    /// </summary>
    /// <param name="requestArgs">Resource server URL and access token for authorization purposes.</param>
    /// <param name="body">Details of the quote including sender's wallet address, receiver information, and payment method.</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation, containing the created quote details.</returns>
    public Task<QuoteResponse> CreateQuoteAsync(
        AuthRequestArgs requestArgs,
        QuoteBody body,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Creates a quote for a payment transaction.
    /// </summary>
    /// <param name="requestArgs">Resource server URL and access token for authorization purposes.</param>
    /// <param name="body">Details of the quote including sender's wallet address, receiver information, and payment method.</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation, containing the created quote details.</returns>
    public Task<QuoteResponse> CreateQuoteAsync(
        AuthRequestArgs requestArgs,
        QuoteBodyWithDebitAmount body,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Creates a quote for a payment transaction.
    /// </summary>
    /// <param name="requestArgs">Resource server URL and access token for authorization purposes.</param>
    /// <param name="body">Details of the quote including sender's wallet address, receiver information, and payment method.</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation, containing the created quote details.</returns>
    public Task<QuoteResponse> CreateQuoteAsync(
        AuthRequestArgs requestArgs,
        QuoteBodyWithReceiveAmount body,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Retrieves a quote from the Open Payments API.
    /// </summary>
    /// <param name="requestArgs">Authentication parameters, including the access token.</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation.</param>
    /// <returns>A <c>QuoteResponse</c> representing the retrieved quote.</returns>
    public Task<QuoteResponse> GetQuoteAsync(
        AuthRequestArgs requestArgs,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Creates an outgoing payment.
    /// </summary>
    /// <param name="requestArgs">Resource server URL address (e.g. <c>https://res.ilp.com/outgoing</c>) and access token.</param>
    /// <param name="body">The request body containing payment details.</param>
    /// <param name="cancellationToken">Optional cancellation token to propagate notification that the operation should be canceled.</param>
    /// <returns>A task that represents the asynchronous operation, containing the response details of the outgoing payment.</returns>
    public Task<OutgoingPaymentWithSpentAmountsResponse> CreateOutgoingPaymentAsync(
        AuthRequestArgs requestArgs,
        OutgoingPaymentBodyFromQuote body,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Creates an outgoing payment.
    /// </summary>
    /// <param name="requestArgs">Resource server URL address (e.g. <c>https://res.ilp.com/outgoing</c>) and access token.</param>
    /// <param name="body">The request body containing payment details.</param>
    /// <param name="cancellationToken">Optional cancellation token to propagate notification that the operation should be canceled.</param>
    /// <returns>A task that represents the asynchronous operation, containing the response details of the outgoing payment.</returns>
    public Task<OutgoingPaymentWithSpentAmountsResponse> CreateOutgoingPaymentAsync(
        AuthRequestArgs requestArgs,
        OutgoingPaymentBodyFromIncomingPayment body,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Retrieves details of an outgoing payment.
    /// </summary>
    /// <param name="requestArgs">Arguments containing the necessary authentication and request context.</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation.</param>
    /// <returns>Details of the requested outgoing payment.</returns>
    public Task<OutgoingPaymentResponse> GetOutgoingPaymentAsync(
        AuthRequestArgs requestArgs,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Lists outgoing payments.
    /// </summary>
    /// <param name="requestArgs">Authentication details and request context.</param>
    /// <param name="query">Query parameters for filtering outgoing payments.</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation.</param>
    /// <returns>A response containing the list of outgoing payments.</returns>
    public Task<ListOutgoingPaymentsResponse> ListOutgoingPaymentsAsync(
        AuthRequestArgs requestArgs,
        ListOutgoingPaymentQuery query,
        CancellationToken cancellationToken = default
    );
}

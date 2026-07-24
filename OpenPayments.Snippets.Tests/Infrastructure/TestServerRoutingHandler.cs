namespace OpenPayments.Snippets.Tests.Infrastructure;

/// <summary>
/// Rewrites every outgoing request's scheme/host/port to <paramref name="targetBaseAddress"/>,
/// leaving path and query untouched. Lets guide code hard-code URLs like
/// <c>https://cloudninebank.example.com/customer</c> while the request actually reaches
/// <see cref="FakeOpenPaymentsServer"/>'s in-memory <c>TestServer</c>. Must be registered
/// after <c>SigningHttpMessageHandler</c> in the HTTP client pipeline so signing still
/// operates on the pre-rewrite request.
/// </summary>
public sealed class TestServerRoutingHandler(Uri targetBaseAddress) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        var original = request.RequestUri!;
        request.RequestUri = new UriBuilder(original)
        {
            Scheme = targetBaseAddress.Scheme,
            Host = targetBaseAddress.Host,
            Port = targetBaseAddress.Port,
        }.Uri;

        return base.SendAsync(request, cancellationToken);
    }
}

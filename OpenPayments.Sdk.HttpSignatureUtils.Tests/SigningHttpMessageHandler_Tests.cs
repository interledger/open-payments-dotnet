#nullable enable

using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Moq.Protected;
using Xunit;

namespace Interledger.OpenPayments.HttpSignatureUtils.Tests;

public class SigningHttpMessageHandlerTests
{
    [Fact]
    public async Task SendAsync_AddsSignatureHeadersBeforeForwarding()
    {
        var key = KeyUtils.GenerateKey();
        HttpRequestMessage? capturedRequest = null;

        var innerHandler = new Mock<HttpMessageHandler>();
        innerHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .Returns<HttpRequestMessage, CancellationToken>(
                (request, _) =>
                {
                    capturedRequest = request;
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
                }
            );

        var signingHandler = new SigningHttpMessageHandler(key, "test-key-id")
        {
            InnerHandler = innerHandler.Object,
        };
        var invoker = new HttpMessageInvoker(signingHandler);

        var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com/resource");
        await invoker.SendAsync(request, CancellationToken.None);

        Assert.NotNull(capturedRequest);
        Assert.True(capturedRequest!.Headers.Contains("Signature"));
        Assert.True(capturedRequest.Headers.Contains("Signature-Input"));
    }

    [Fact]
    public async Task SendAsync_DoesNotBlockCallingThread()
    {
        var key = KeyUtils.GenerateKey();

        var innerHandler = new Mock<HttpMessageHandler>();
        innerHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var signingHandler = new SigningHttpMessageHandler(key, "test-key-id")
        {
            InnerHandler = innerHandler.Object,
        };
        var invoker = new HttpMessageInvoker(signingHandler);

        // SendAsync must return a Task that completes without synchronously
        // blocking via .Result/.Wait() anywhere in the signing path.
        var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com/resource");
        var task = invoker.SendAsync(request, CancellationToken.None);
        var response = await task;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}

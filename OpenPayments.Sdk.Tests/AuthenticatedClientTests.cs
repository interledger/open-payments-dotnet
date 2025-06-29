using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using OpenPayments.Sdk;
using OpenPayments.Sdk.Generated.Resource;
using Xunit;

namespace OpenPayments.Sdk.Tests;

public class AuthenticatedClientTests
{
    [Fact]
    public async Task RequestGrantAsync_SendsSignedRequest()
    {
        // Arrange
        var privateBytes = new byte[64]; // dummy key
        var keyId = "test-kid";
        var expectedUri = "https://auth.example/grant";

        var handler = new CaptureHandler(HttpMethod.Post, expectedUri, new { ok = true });
        var http = new HttpClient(handler, disposeHandler: true);
        var client = new AuthenticatedClient(http, "https://wallet.example/alice", privateBytes, keyId);

        // Act
        try
        {
            var resp = await client.RequestGrantAsync(new Uri(expectedUri), new { dummy = 1 });

            Assert.NotNull(handler.LastRequest);
            Assert.True(handler.LastRequest!.Headers.Contains("Signature"));
            Assert.True(handler.LastRequest!.Headers.Contains("Signature-Input"));
            // No Authorization header for initial grant request
            Assert.False(handler.LastRequest!.Headers.Contains("Authorization"));
        }
        catch (PlatformNotSupportedException)
        {
            return; // libsodium unavailable – skip
        }
    }

    [Fact]
    public async Task CreateIncomingPaymentAsync_SendsSignedAuthorizedRequest()
    {
        // Arrange
        var privateBytes = new byte[64];
        var keyId = "kid1";
        var accessToken = "TESTTOKEN";

        var expectedEndpoint = "https://resource.example/incoming-payments";
        var handler = new CaptureHandler(HttpMethod.Post, HttpStatusCode.Created, expectedEndpoint, new { id = "ok" });
        var http = new HttpClient(handler, disposeHandler: true);
        var authClient = new AuthenticatedClient(http, "https://wallet.example/alice", privateBytes, keyId);

        var body = new Body
        {
            WalletAddress = new Uri("https://wallet.example/alice")
        };

        // Act
        try
        {
            var _ = await authClient.CreateIncomingPaymentAsync(new Uri("https://resource.example"), body, accessToken);

            var req = handler.LastRequest;
            Assert.NotNull(req);
            Assert.Equal("GNAP " + accessToken, req!.Headers.Authorization!.ToString());
            Assert.True(req.Headers.Contains("Signature"));
            Assert.True(req.Headers.Contains("Signature-Input"));
        }
        catch (PlatformNotSupportedException)
        {
            return; // skip when libsodium missing
        }
    }

    private sealed class CaptureHandler : HttpMessageHandler
    {
        private readonly HttpMethod _method;
        private readonly string _expectedUri;
        private readonly string _json;
        public HttpRequestMessage? LastRequest { get; private set; }
        private readonly HttpStatusCode _expectedStatusCode;

        public CaptureHandler(HttpMethod method, string expectedUri, object responseObj): this(method, HttpStatusCode.OK, expectedUri, responseObj)
        {
            
        }

        public CaptureHandler(HttpMethod method, HttpStatusCode statusCode, string expectedUri, object responseObj)
        {
            _method = method;
            _expectedUri = expectedUri;
            _expectedStatusCode = statusCode;
            _json = JsonConvert.SerializeObject(responseObj);
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            Assert.Equal(_method, request.Method);
            Assert.Equal(_expectedUri, request.RequestUri!.ToString());

            var response = new HttpResponseMessage(_expectedStatusCode)
            {
                Content = new StringContent(_json, Encoding.UTF8, "application/json")
            };
            return Task.FromResult(response);
        }
    }
} 
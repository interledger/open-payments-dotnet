using System.Collections.Concurrent;
using System.Net;
using System.Text;
using FluentAssertions;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using Interledger.OpenPayments.Clients;
using Interledger.OpenPayments.Generated.Auth;

namespace Interledger.OpenPayments.Tests.Clients;

public class AuthClientBase_ConcurrencyTests
{
    private const int Iterations = 500;

    [Fact]
    [Trait("Category", "Slow")]
    public async Task RequestGrantAsync_ConcurrentCallsToDifferentHosts_NeverCrossesHosts()
    {
        var observations = new ConcurrentBag<(string ExpectedHost, string ActualHost)>();

        var handler = new Mock<HttpMessageHandler>();
        handler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .Returns<HttpRequestMessage, CancellationToken>(
                (request, _) =>
                {
                    var json = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
                    var body = JsonConvert.DeserializeObject<GrantCreateBody>(json)!;
                    var expectedHost = body.Client!.Host;
                    observations.Add((expectedHost, request.RequestUri!.Host));

                    var response = new AuthResponse
                    {
                        AccessToken = new AccessTokenResponse
                        {
                            Access = [new IncomingAccess { Actions = [Actions.Read] }],
                        },
                    };

                    return Task.FromResult(
                        new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent(
                                JsonConvert.SerializeObject(response),
                                Encoding.UTF8,
                                "application/json"
                            ),
                        }
                    );
                }
            );

        var httpClient = new HttpClient(handler.Object);
        const string hostA = "auth-a.example";
        const string hostB = "auth-b.example";

        var ready = new CountdownEvent(Iterations);
        var gate = new ManualResetEventSlim(false);

        var calls = Enumerable
            .Range(0, Iterations)
            .Select(i =>
            {
                var host = i % 2 == 0 ? hostA : hostB;
                var clientUrl = new Uri($"https://{host}/client");
                var client = new AuthClientBase(httpClient, clientUrl);
                return Task.Run(async () =>
                {
                    ready.Signal();
                    gate.Wait();
                    await client.RequestGrantAsync(
                        new RequestArgs { Url = new Uri($"https://{host}/auth") },
                        new GrantCreateBody
                        {
                            AccessToken = new AccessToken
                            {
                                Access = [new IncomingAccess { Actions = [Actions.Read] }],
                            },
                        }
                    );
                });
            })
            .ToArray();

        ready.Wait();
        gate.Set();
        await Task.WhenAll(calls);

        observations.Should().OnlyContain(o => o.ActualHost == o.ExpectedHost);
    }
}

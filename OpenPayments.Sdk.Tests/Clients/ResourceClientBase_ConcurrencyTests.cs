using System.Collections.Concurrent;
using System.Net;
using System.Text;
using AwesomeAssertions;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using OpenPayments.Sdk.Clients;
using OpenPayments.Sdk.Generated.Resource;

namespace OpenPayments.Sdk.Tests.Clients;

public class ResourceClientBase_ConcurrencyTests
{
    private const int Iterations = 500;

    [Fact]
    [Trait("Category", "Slow")]
    public async Task GetIncomingPaymentAsync_ConcurrentCallsToDifferentHosts_NeverCrossesHosts()
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
                    var expectedHost = request.Headers.Authorization!.Parameter!;
                    observations.Add((expectedHost, request.RequestUri!.Host));

                    var body = new IncomingPaymentResponse
                    {
                        Id = new Uri($"https://{expectedHost}/incoming-payments/1"),
                        WalletAddress = new Uri($"https://{expectedHost}/alice"),
                        ReceivedAmount = new Amount("0", "EUR", 2),
                        Completed = false,
                        CreatedAt = DateTime.UtcNow,
                        Methods =
                        [
                            new IlpPaymentMethod
                            {
                                Type = IlpPaymentMethodType.Ilp,
                                IlpAddress = $"{expectedHost}.incoming-payments.1",
                                SharedSecret = "secret",
                            },
                        ],
                    };

                    return Task.FromResult(
                        new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent(
                                JsonConvert.SerializeObject(body),
                                Encoding.UTF8,
                                "application/json"
                            ),
                        }
                    );
                }
            );

        var httpClient = new HttpClient(handler.Object);
        var client = new ResourceClientBase(httpClient, new Uri("https://client.example"));

        const string hostA = "host-a.example";
        const string hostB = "host-b.example";

        // Without this, the ThreadPool ramps up ~1-2 workers/sec under sustained
        // blocking, turning this test's 500 concurrent Task.Run + gate.Wait() into
        // a multi-minute run instead of a real concurrency stress test.
        ThreadPool.GetMinThreads(out var workerThreads, out var ioThreads);
        ThreadPool.SetMinThreads(Math.Max(workerThreads, Iterations), ioThreads);

        var ready = new CountdownEvent(Iterations);
        var gate = new ManualResetEventSlim(false);

        var calls = Enumerable
            .Range(0, Iterations)
            .Select(i =>
            {
                var host = i % 2 == 0 ? hostA : hostB;
                return Task.Run(async () =>
                {
                    ready.Signal();
                    gate.Wait();
                    await client.GetIncomingPaymentAsync(
                        new AuthRequestArgs
                        {
                            Url = new Uri($"https://{host}/incoming-payments/1"),
                            AccessToken = host,
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

using System.Net;
using System.Text;
using FluentAssertions;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using Interledger.OpenPayments.Clients;
using Interledger.OpenPayments.Generated.Resource;

namespace Interledger.OpenPayments.Tests.Clients;

public class ResourceClientBase_PagingTests
{
    internal static IncomingPayment MakePayment(int i) =>
        new()
        {
            Id = new Uri($"https://host-a.example/incoming-payments/{i}"),
            WalletAddress = new Uri("https://host-a.example/alice"),
            ReceivedAmount = new Amount("0", "EUR", 2),
            Completed = false,
            CreatedAt = DateTime.UtcNow,
        };

    internal static string? GetQueryValue(Uri uri, string name) =>
        uri.Query.TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(pair => pair.Split('=', 2))
            .Where(parts => parts[0] == name)
            .Select(parts => Uri.UnescapeDataString(parts[1]))
            .FirstOrDefault();

    internal static (HttpClient Client, List<Uri> Requests) CreateClient(
        Func<string?, object> pageForCursor
    )
    {
        var requests = new List<Uri>();
        var handler = new Mock<HttpMessageHandler>();
        handler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .Returns<HttpRequestMessage, CancellationToken>(
                (request, cancellationToken) =>
                {
                    // Real handlers (e.g. SocketsHttpHandler) observe the token before
                    // dispatching a request; mirror that here so cancellation tests are
                    // meaningful rather than relying on an unobserved token flowing through.
                    cancellationToken.ThrowIfCancellationRequested();

                    lock (requests)
                        requests.Add(request.RequestUri!);

                    var page = pageForCursor(GetQueryValue(request.RequestUri!, "cursor"));

                    return Task.FromResult(
                        new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent(
                                JsonConvert.SerializeObject(page),
                                Encoding.UTF8,
                                "application/json"
                            ),
                        }
                    );
                }
            );

        return (new HttpClient(handler.Object), requests);
    }

    private static (HttpClient Client, List<Uri> Requests) CreateTwoPageClient() =>
        CreateClient(cursor =>
            cursor switch
            {
                null => new ListIncomingPaymentsResponse
                {
                    Result = [MakePayment(1), MakePayment(2)],
                    Pagination = new PageInfo
                    {
                        EndCursor = "cursor-1",
                        HasNextPage = true,
                        HasPreviousPage = false,
                    },
                },
                "cursor-1" => new ListIncomingPaymentsResponse
                {
                    Result = [MakePayment(3)],
                    Pagination = new PageInfo
                    {
                        StartCursor = "cursor-1",
                        EndCursor = "cursor-2",
                        HasNextPage = false,
                        HasPreviousPage = true,
                    },
                },
                _ => throw new InvalidOperationException($"Unexpected cursor: {cursor}"),
            }
        );

    private static AuthRequestArgs Args() =>
        new() { Url = new Uri("https://host-a.example/"), AccessToken = "token" };

    [Fact]
    public async Task ListIncomingPaymentsAllAsync_FollowsCursorsAcrossAllPages()
    {
        var (httpClient, requests) = CreateTwoPageClient();
        var client = new ResourceClientBase(httpClient, new Uri("https://client.example"));

        var payments = new List<IncomingPayment>();
        await foreach (
            var payment in client.ListIncomingPaymentsAllAsync(
                Args(),
                new ListIncomingPaymentQuery { WalletAddress = "https://host-a.example/alice" }
            )
        )
        {
            payments.Add(payment);
        }

        payments
            .Select(p => p.Id.ToString())
            .Should()
            .Equal(
                "https://host-a.example/incoming-payments/1",
                "https://host-a.example/incoming-payments/2",
                "https://host-a.example/incoming-payments/3"
            );

        requests.Should().HaveCount(2);
        GetQueryValue(requests[0], "cursor").Should().BeNull();
        GetQueryValue(requests[1], "cursor").Should().Be("cursor-1");
    }

    [Fact]
    public async Task ListIncomingPaymentsAllAsync_StartsFromCallerCursorAndKeepsFirst()
    {
        var (httpClient, requests) = CreateTwoPageClient();
        var client = new ResourceClientBase(httpClient, new Uri("https://client.example"));

        var payments = new List<IncomingPayment>();
        await foreach (
            var payment in client.ListIncomingPaymentsAllAsync(
                Args(),
                new ListIncomingPaymentQuery
                {
                    WalletAddress = "https://host-a.example/alice",
                    Cursor = "cursor-1",
                    First = 25,
                }
            )
        )
        {
            payments.Add(payment);
        }

        payments.Should().HaveCount(1);
        requests.Should().HaveCount(1);
        GetQueryValue(requests[0], "cursor").Should().Be("cursor-1");
        GetQueryValue(requests[0], "first").Should().Be("25");
    }

    [Fact]
    public async Task ListIncomingPaymentsAllAsync_LastSet_ThrowsArgumentException()
    {
        var (httpClient, requests) = CreateTwoPageClient();
        var client = new ResourceClientBase(httpClient, new Uri("https://client.example"));

        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await foreach (
                var _ in client.ListIncomingPaymentsAllAsync(
                    Args(),
                    new ListIncomingPaymentQuery
                    {
                        WalletAddress = "https://host-a.example/alice",
                        Last = 5,
                    }
                )
            ) { }
        });

        requests.Should().BeEmpty();
    }

    [Fact]
    public async Task ListIncomingPaymentsAllAsync_ServerRepeatsCursor_Throws()
    {
        var (httpClient, _) = CreateClient(_ => new ListIncomingPaymentsResponse
        {
            Result = [MakePayment(1)],
            Pagination = new PageInfo
            {
                EndCursor = "stuck",
                HasNextPage = true,
                HasPreviousPage = false,
            },
        });
        var client = new ResourceClientBase(httpClient, new Uri("https://client.example"));

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (
                var _ in client.ListIncomingPaymentsAllAsync(
                    Args(),
                    new ListIncomingPaymentQuery { WalletAddress = "https://host-a.example/alice" }
                )
            ) { }
        });
    }

    [Fact]
    public async Task ListIncomingPaymentsAllAsync_CanceledMidEnumeration_ThrowsAndStopsFetching()
    {
        var (httpClient, requests) = CreateTwoPageClient();
        var client = new ResourceClientBase(httpClient, new Uri("https://client.example"));

        using var cts = new CancellationTokenSource();
        var payments = new List<IncomingPayment>();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await foreach (
                var payment in client.ListIncomingPaymentsAllAsync(
                    Args(),
                    new ListIncomingPaymentQuery { WalletAddress = "https://host-a.example/alice" },
                    cts.Token
                )
            )
            {
                payments.Add(payment);
                if (payments.Count == 2)
                    cts.Cancel();
            }
        });

        payments.Should().HaveCount(2);
        requests.Should().HaveCount(1);
    }

    private static OutgoingPayment MakeOutgoingPayment(int i) =>
        new()
        {
            Id = new Uri($"https://host-a.example/outgoing-payments/{i}"),
            WalletAddress = new Uri("https://host-a.example/alice"),
            Receiver = new Uri("https://host-b.example/incoming-payments/1"),
            ReceiveAmount = new Amount("100", "EUR", 2),
            DebitAmount = new Amount("101", "EUR", 2),
            SentAmount = new Amount("0", "EUR", 2),
            CreatedAt = DateTime.UtcNow,
        };

    [Fact]
    public async Task ListOutgoingPaymentsAllAsync_FollowsCursorsAcrossAllPages()
    {
        var (httpClient, requests) = CreateClient(cursor =>
            cursor switch
            {
                null => new ListOutgoingPaymentsResponse
                {
                    Result = [MakeOutgoingPayment(1), MakeOutgoingPayment(2)],
                    Pagination = new PageInfo
                    {
                        EndCursor = "cursor-1",
                        HasNextPage = true,
                        HasPreviousPage = false,
                    },
                },
                _ => new ListOutgoingPaymentsResponse
                {
                    Result = [MakeOutgoingPayment(3)],
                    Pagination = new PageInfo
                    {
                        EndCursor = "cursor-2",
                        HasNextPage = false,
                        HasPreviousPage = true,
                    },
                },
            }
        );
        var client = new ResourceClientBase(httpClient, new Uri("https://client.example"));

        var payments = new List<OutgoingPayment>();
        await foreach (
            var payment in client.ListOutgoingPaymentsAllAsync(
                Args(),
                new ListOutgoingPaymentQuery { WalletAddress = "https://host-a.example/alice" }
            )
        )
        {
            payments.Add(payment);
        }

        payments
            .Select(p => p.Id.ToString())
            .Should()
            .Equal(
                "https://host-a.example/outgoing-payments/1",
                "https://host-a.example/outgoing-payments/2",
                "https://host-a.example/outgoing-payments/3"
            );

        requests.Should().HaveCount(2);
        GetQueryValue(requests[1], "cursor").Should().Be("cursor-1");
    }

    [Fact]
    public async Task ListOutgoingPaymentsAllAsync_LastSet_ThrowsArgumentException()
    {
        var (httpClient, requests) = CreateTwoPageClient();
        var client = new ResourceClientBase(httpClient, new Uri("https://client.example"));

        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await foreach (
                var _ in client.ListOutgoingPaymentsAllAsync(
                    Args(),
                    new ListOutgoingPaymentQuery
                    {
                        WalletAddress = "https://host-a.example/alice",
                        Last = 5,
                    }
                )
            ) { }
        });

        requests.Should().BeEmpty();
    }
}

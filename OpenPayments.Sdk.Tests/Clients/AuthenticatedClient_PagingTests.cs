using AwesomeAssertions;
using Interledger.OpenPayments.Clients;
using Interledger.OpenPayments.Generated.Resource;

namespace Interledger.OpenPayments.Tests.Clients;

public class AuthenticatedClient_PagingTests
{
    [Fact]
    public async Task ListIncomingPaymentsAllAsync_EnumeratesAcrossPages()
    {
        var (httpClient, requests) = ResourceClientBase_PagingTests.CreateClient(cursor =>
            cursor switch
            {
                null => new ListIncomingPaymentsResponse
                {
                    Result =
                    [
                        ResourceClientBase_PagingTests.MakePayment(1),
                        ResourceClientBase_PagingTests.MakePayment(2),
                    ],
                    Pagination = new PageInfo
                    {
                        EndCursor = "cursor-1",
                        HasNextPage = true,
                        HasPreviousPage = false,
                    },
                },
                _ => new ListIncomingPaymentsResponse
                {
                    Result = [ResourceClientBase_PagingTests.MakePayment(3)],
                    Pagination = new PageInfo
                    {
                        EndCursor = "cursor-2",
                        HasNextPage = false,
                        HasPreviousPage = true,
                    },
                },
            }
        );
        var client = new AuthenticatedClient(httpClient, new Uri("https://client.example"));

        var ids = new List<string>();
        await foreach (
            var payment in client.ListIncomingPaymentsAllAsync(
                new AuthRequestArgs
                {
                    Url = new Uri("https://host-a.example/"),
                    AccessToken = "token",
                },
                new ListIncomingPaymentQuery { WalletAddress = "https://host-a.example/alice" }
            )
        )
        {
            ids.Add(payment.Id.ToString());
        }

        ids.Should().HaveCount(3);
        requests.Should().HaveCount(2);
    }

    [Fact]
    public async Task ListOutgoingPaymentsAllAsync_EnumeratesAcrossPages()
    {
        var (httpClient, requests) = ResourceClientBase_PagingTests.CreateClient(cursor =>
            cursor switch
            {
                null => new ListOutgoingPaymentsResponse
                {
                    Result =
                    [
                        ResourceClientBase_PagingTests.MakeOutgoingPayment(1),
                        ResourceClientBase_PagingTests.MakeOutgoingPayment(2),
                    ],
                    Pagination = new PageInfo
                    {
                        EndCursor = "cursor-1",
                        HasNextPage = true,
                        HasPreviousPage = false,
                    },
                },
                _ => new ListOutgoingPaymentsResponse
                {
                    Result = [ResourceClientBase_PagingTests.MakeOutgoingPayment(3)],
                    Pagination = new PageInfo
                    {
                        EndCursor = "cursor-2",
                        HasNextPage = false,
                        HasPreviousPage = true,
                    },
                },
            }
        );
        var client = new AuthenticatedClient(httpClient, new Uri("https://client.example"));

        var ids = new List<string>();
        await foreach (
            var payment in client.ListOutgoingPaymentsAllAsync(
                new AuthRequestArgs
                {
                    Url = new Uri("https://host-a.example/"),
                    AccessToken = "token",
                },
                new ListOutgoingPaymentQuery { WalletAddress = "https://host-a.example/alice" }
            )
        )
        {
            ids.Add(payment.Id.ToString());
        }

        ids.Should().HaveCount(3);
        requests.Should().HaveCount(2);
    }
}

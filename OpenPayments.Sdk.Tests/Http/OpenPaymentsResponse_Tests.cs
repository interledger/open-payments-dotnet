using System.Net;
using System.Text;
using FluentAssertions;
using OpenPayments.Sdk.Exceptions;
using OpenPayments.Sdk.Http;

namespace OpenPayments.Sdk.Tests.Http;

public class OpenPaymentsResponse_ThrowIfError_Tests
{
    private static HttpResponseMessage Response(
        HttpStatusCode status,
        string body,
        params (string Name, string Value)[] headers
    )
    {
        var response = new HttpResponseMessage(status)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };

        foreach (var (name, value) in headers)
            response.Headers.TryAddWithoutValidation(name, value);

        return response;
    }

    [Theory]
    [InlineData(HttpStatusCode.OK)]
    [InlineData(HttpStatusCode.Created)]
    [InlineData(HttpStatusCode.NoContent)]
    public async Task ThrowIfErrorAsync_OnSuccess_DoesNotThrow(HttpStatusCode status)
    {
        using var response = Response(status, "{}");

        var act = () => OpenPaymentsResponse.ThrowIfErrorAsync(response, CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ThrowIfErrorAsync_WithResourceShapedBody_MapsEveryField()
    {
        var body = """{"error":{"code":"unauthorized","description":"Access token is invalid"}}""";
        using var response = Response(HttpStatusCode.Unauthorized, body);

        var exception = await Assert.ThrowsAsync<OpenPaymentsApiException>(() =>
            OpenPaymentsResponse.ThrowIfErrorAsync(response, CancellationToken.None)
        );

        exception.StatusCode.Should().Be(401);
        exception.ErrorCode.Should().Be("unauthorized");
        exception.Description.Should().Be("Access token is invalid");
        exception.ResponseBody.Should().Be(body);
    }

    [Fact]
    public async Task ThrowIfErrorAsync_WithAuthShapedBody_ReadsTheGnapCodeAsItsWireString()
    {
        var body = """{"error":{"code":"invalid_client","description":"Client is not valid"}}""";
        using var response = Response(HttpStatusCode.BadRequest, body);

        var exception = await Assert.ThrowsAsync<OpenPaymentsApiException>(() =>
            OpenPaymentsResponse.ThrowIfErrorAsync(response, CancellationToken.None)
        );

        exception.ErrorCode.Should().Be("invalid_client");
        exception.Description.Should().Be("Client is not valid");
    }

    [Fact]
    public async Task ThrowIfErrorAsync_WithGnapBareStringError_ReadsItAsTheCode()
    {
        using var response = Response(HttpStatusCode.BadRequest, """{"error":"invalid_request"}""");

        var exception = await Assert.ThrowsAsync<OpenPaymentsApiException>(() =>
            OpenPaymentsResponse.ThrowIfErrorAsync(response, CancellationToken.None)
        );

        exception.ErrorCode.Should().Be("invalid_request");
        exception.Description.Should().BeNull();
    }

    [Theory]
    [InlineData("""{"error":{}}""", null, null)]
    [InlineData("""{"error":{"code":"only_code"}}""", "only_code", null)]
    [InlineData("""{"error":{"description":"only description"}}""", null, "only description")]
    [InlineData("""{"unexpected":"shape"}""", null, null)]
    [InlineData("""{"error":{"code":{"nested":"object"}}}""", null, null)]
    [InlineData("""{"error":{"code":[1,2,3]}}""", null, null)]
    [InlineData("""{"error":{"description":{"nested":"object"}}}""", null, null)]
    [InlineData("""{"error":{"description":[1,2,3]}}""", null, null)]
    [InlineData("""{"error":{"code":123}}""", null, null)]
    [InlineData("""{"error":{"description":true}}""", null, null)]
    public async Task ThrowIfErrorAsync_WithPartialBody_MapsWhatIsPresent(
        string body,
        string? expectedCode,
        string? expectedDescription
    )
    {
        using var response = Response(HttpStatusCode.BadRequest, body);

        var exception = await Assert.ThrowsAsync<OpenPaymentsApiException>(() =>
            OpenPaymentsResponse.ThrowIfErrorAsync(response, CancellationToken.None)
        );

        exception.ErrorCode.Should().Be(expectedCode);
        exception.Description.Should().Be(expectedDescription);
        exception.ResponseBody.Should().Be(body);
    }

    [Theory]
    [InlineData("<html><body>Too Many Requests</body></html>")]
    [InlineData("")]
    [InlineData("not json at all")]
    [InlineData("[1,2,3]")]
    public async Task ThrowIfErrorAsync_WithNonConformingBody_KeepsTheBodyAndNullsTheFields(
        string body
    )
    {
        using var response = Response(HttpStatusCode.TooManyRequests, body);

        var exception = await Assert.ThrowsAsync<OpenPaymentsApiException>(() =>
            OpenPaymentsResponse.ThrowIfErrorAsync(response, CancellationToken.None)
        );

        exception.StatusCode.Should().Be(429);
        exception.ErrorCode.Should().BeNull();
        exception.Description.Should().BeNull();
        exception.ResponseBody.Should().Be(body);
        exception.Message.Should().Be("The Open Payments request failed with HTTP 429.");
    }

    [Fact]
    public async Task ThrowIfErrorAsync_On429WithDeltaSeconds_ParsesRetryAfter()
    {
        using var response = Response(
            HttpStatusCode.TooManyRequests,
            """{"error":{"code":"too_fast","description":"Slow down"}}""",
            ("Retry-After", "30")
        );

        var exception = await Assert.ThrowsAsync<OpenPaymentsApiException>(() =>
            OpenPaymentsResponse.ThrowIfErrorAsync(response, CancellationToken.None)
        );

        exception.StatusCode.Should().Be(429);
        exception.ErrorCode.Should().Be("too_fast");
        exception.RetryAfter.Should().Be(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public async Task ThrowIfErrorAsync_On429WithFutureHttpDate_ParsesRetryAfterAsRemainingTime()
    {
        var future = DateTimeOffset.UtcNow.AddMinutes(5).ToString("R");
        using var response = Response(HttpStatusCode.TooManyRequests, "{}", ("Retry-After", future));

        var exception = await Assert.ThrowsAsync<OpenPaymentsApiException>(() =>
            OpenPaymentsResponse.ThrowIfErrorAsync(response, CancellationToken.None)
        );

        exception
            .RetryAfter.Should()
            .BeGreaterThan(TimeSpan.FromMinutes(4))
            .And.BeLessThanOrEqualTo(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public async Task ThrowIfErrorAsync_On429WithPastHttpDate_ClampsRetryAfterToZero()
    {
        var past = DateTimeOffset.UtcNow.AddMinutes(-5).ToString("R");
        using var response = Response(HttpStatusCode.TooManyRequests, "{}", ("Retry-After", past));

        var exception = await Assert.ThrowsAsync<OpenPaymentsApiException>(() =>
            OpenPaymentsResponse.ThrowIfErrorAsync(response, CancellationToken.None)
        );

        exception.RetryAfter.Should().Be(TimeSpan.Zero);
    }

    [Theory]
    [InlineData("wildly unparseable")]
    [InlineData("")]
    public async Task ThrowIfErrorAsync_WithUnusableRetryAfter_LeavesItNull(string headerValue)
    {
        using var response = Response(
            HttpStatusCode.TooManyRequests,
            "{}",
            ("Retry-After", headerValue)
        );

        var exception = await Assert.ThrowsAsync<OpenPaymentsApiException>(() =>
            OpenPaymentsResponse.ThrowIfErrorAsync(response, CancellationToken.None)
        );

        exception.RetryAfter.Should().BeNull();
    }

    [Fact]
    public async Task ThrowIfErrorAsync_On500WithEmptyBody_StillCarriesTheStatus()
    {
        using var response = Response(HttpStatusCode.InternalServerError, "");

        var exception = await Assert.ThrowsAsync<OpenPaymentsApiException>(() =>
            OpenPaymentsResponse.ThrowIfErrorAsync(response, CancellationToken.None)
        );

        exception.StatusCode.Should().Be(500);
        exception.ErrorCode.Should().BeNull();
        exception.ResponseBody.Should().BeEmpty();
    }

    [Fact]
    public async Task ThrowIfErrorAsync_CapturesResponseHeaders()
    {
        using var response = Response(
            HttpStatusCode.Forbidden,
            "{}",
            ("X-Request-Id", "abc-123")
        );

        var exception = await Assert.ThrowsAsync<OpenPaymentsApiException>(() =>
            OpenPaymentsResponse.ThrowIfErrorAsync(response, CancellationToken.None)
        );

        exception.Headers.Should().ContainKey("X-Request-ID");
        exception.Headers["X-Request-ID"].Should().ContainSingle().Which.Should().Be("abc-123");
    }
}

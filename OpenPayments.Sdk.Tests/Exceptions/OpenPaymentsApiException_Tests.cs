using FluentAssertions;
using OpenPayments.Sdk.Exceptions;

namespace OpenPayments.Sdk.Tests.Exceptions;

public class OpenPaymentsApiException_Tests
{
    [Fact]
    public void Message_WithDescriptionAndCode_IncludesBoth()
    {
        var exception = new OpenPaymentsApiException(
            403,
            "forbidden",
            "Access token is not permitted to perform this action",
            "{}"
        );

        exception
            .Message.Should()
            .Be(
                "Access token is not permitted to perform this action (HTTP 403, code: forbidden)"
            );
    }

    [Fact]
    public void Message_WithDescriptionOnly_OmitsCodeClause()
    {
        var exception = new OpenPaymentsApiException(500, null, "Something broke", "{}");

        exception.Message.Should().Be("Something broke (HTTP 500)");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Message_WithoutDescription_FallsBackToStatus(string? description)
    {
        var exception = new OpenPaymentsApiException(429, null, description, "");

        exception.Message.Should().Be("The Open Payments request failed with HTTP 429.");
    }

    [Fact]
    public void Message_DoesNotIncludeTheResponseBody()
    {
        var body = new string('x', 2000);

        var exception = new OpenPaymentsApiException(400, "invalid_request", "Bad request", body);

        exception.Message.Should().NotContain("xxx");
        exception.ResponseBody.Should().Be(body).And.HaveLength(2000);
    }

    [Fact]
    public void Properties_AreExposedVerbatim()
    {
        var headers = new Dictionary<string, IEnumerable<string>>
        {
            ["Retry-After"] = new[] { "30" },
        };

        var exception = new OpenPaymentsApiException(
            429,
            "too_fast",
            "Slow down",
            "{\"error\":{}}",
            headers,
            TimeSpan.FromSeconds(30)
        );

        exception.StatusCode.Should().Be(429);
        exception.ErrorCode.Should().Be("too_fast");
        exception.Description.Should().Be("Slow down");
        exception.ResponseBody.Should().Be("{\"error\":{}}");
        exception.Headers.Should().ContainKey("Retry-After");
        exception.RetryAfter.Should().Be(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void Headers_WhenNotSupplied_IsEmptyNotNull()
    {
        var exception = new OpenPaymentsApiException(404, null, null, null);

        exception.Headers.Should().NotBeNull().And.BeEmpty();
        exception.RetryAfter.Should().BeNull();
        exception.ResponseBody.Should().BeNull();
    }

    [Fact]
    public void InnerException_IsPreserved()
    {
        var inner = new InvalidOperationException("boom");

        var exception = new OpenPaymentsApiException(
            200,
            null,
            "Could not deserialize",
            "{",
            innerException: inner
        );

        exception.InnerException.Should().BeSameAs(inner);
    }
}

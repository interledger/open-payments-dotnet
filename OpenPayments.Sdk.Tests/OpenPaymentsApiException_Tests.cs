using AwesomeAssertions;

namespace OpenPayments.Sdk.Tests;

public class OpenPaymentsApiExceptionTests
{
    [Fact]
    public void Create_SetsAllProperties()
    {
        var headers = new Dictionary<string, IEnumerable<string>>
        {
            ["Content-Type"] = new[] { "application/json" },
        };

        var exception = OpenPaymentsExceptionFactory.Create(
            "invalid_request",
            400,
            "invalid_request",
            "{\"error\":{\"code\":\"invalid_request\"}}",
            headers
        );

        exception.Message.Should().Be("invalid_request");
        exception.StatusCode.Should().Be(400);
        exception.ErrorCode.Should().Be("invalid_request");
        exception.RawResponse.Should().Be("{\"error\":{\"code\":\"invalid_request\"}}");
        exception.Headers.Should().BeSameAs(headers);
    }

    [Fact]
    public void Create_AllowsNullErrorCodeAndRawResponse()
    {
        var headers = new Dictionary<string, IEnumerable<string>>();

        var exception = OpenPaymentsExceptionFactory.Create(
            "Response was null which was not expected.",
            500,
            null,
            null,
            headers
        );

        exception.ErrorCode.Should().BeNull();
        exception.RawResponse.Should().BeNull();
    }

    [Fact]
    public void ToString_IncludesStatusCodeAndRawResponse()
    {
        var exception = OpenPaymentsExceptionFactory.Create(
            "boom",
            503,
            null,
            "raw body",
            new Dictionary<string, IEnumerable<string>>()
        );

        exception.ToString().Should().Contain("503").And.Contain("raw body");
    }
}

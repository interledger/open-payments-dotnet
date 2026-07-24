using AwesomeAssertions;
using OpenPayments.Snippets.Services;

namespace OpenPayments.Snippets.Tests.Services;

public class GrantInteractionListener_Tests
{
    [Fact]
    public async Task WaitForCallbackAsync_ParsesInteractRefAndHashFromCallbackRequest()
    {
        const int port = 34519;
        using var listener = new GrantInteractionListener();
        await listener.StartAsync(port);

        var waitTask = listener.WaitForCallbackAsync(TimeSpan.FromSeconds(5));

        using var httpClient = new HttpClient();
        var response = await httpClient.GetAsync(
            $"http://localhost:{port}/callback/?interact_ref=abc123&hash=deadbeef"
        );
        response.EnsureSuccessStatusCode();

        var callback = await waitTask;

        callback.InteractRef.Should().Be("abc123");
        callback.Hash.Should().Be("deadbeef");
    }

    [Fact]
    public async Task WaitForCallbackAsync_ThrowsTimeoutExceptionWhenNoCallbackArrives()
    {
        const int port = 34520;
        using var listener = new GrantInteractionListener();
        await listener.StartAsync(port);

        var act = async () => await listener.WaitForCallbackAsync(TimeSpan.FromMilliseconds(200));

        await act.Should().ThrowAsync<TimeoutException>();
    }
}

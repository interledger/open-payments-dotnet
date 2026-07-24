using System.Net;
using System.Text;

namespace OpenPayments.Snippets.Services;

public class GrantInteractionCallback
{
    public string? InteractRef { get; set; }
    public string? Hash { get; set; }
}

public sealed class GrantInteractionListener : IDisposable
{
    private const string CallbackHtml =
        "<html><body>You can close this window and return to the terminal.</body></html>";

    private HttpListener? _listener;

    public Task StartAsync(int port)
    {
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://localhost:{port}/callback/");
        _listener.Start();
        return Task.CompletedTask;
    }

    public async Task<GrantInteractionCallback> WaitForCallbackAsync(TimeSpan timeout)
    {
        if (_listener == null)
            throw new InvalidOperationException(
                "StartAsync must be called before WaitForCallbackAsync."
            );

        var contextTask = _listener.GetContextAsync();
        var timeoutTask = Task.Delay(timeout);
        var completed = await Task.WhenAny(contextTask, timeoutTask);

        if (completed == timeoutTask)
            throw new TimeoutException(
                $"Timed out after {timeout} waiting for the grant interaction callback."
            );

        var context = await contextTask;
        var query = context.Request.QueryString;
        var callback = new GrantInteractionCallback
        {
            InteractRef = query["interact_ref"],
            Hash = query["hash"],
        };

        var buffer = Encoding.UTF8.GetBytes(CallbackHtml);
        context.Response.ContentType = "text/html";
        context.Response.ContentLength64 = buffer.Length;
        await context.Response.OutputStream.WriteAsync(buffer);
        context.Response.OutputStream.Close();

        _listener.Stop();

        return callback;
    }

    public void Dispose()
    {
        _listener?.Close();
    }
}

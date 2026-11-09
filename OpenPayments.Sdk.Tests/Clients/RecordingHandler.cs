using System.Net;
using System.Text;
using Newtonsoft.Json;

namespace OpenPayments.Sdk.Tests.Clients;

/// <summary>
/// Records the absolute URI of every request and returns a freshly built canned response.
/// A new response per call matters: the parallel tests read each response body, and a shared
/// instance would have its content stream consumed by the first reader.
/// </summary>
public sealed class RecordingHandler(object? responseObject = null, HttpStatusCode? statusCode = null)
    : HttpMessageHandler
{
    private readonly List<Uri> _requestUris = [];
    private readonly Lock _gate = new();

    private readonly HttpStatusCode _statusCode =
        statusCode ?? (responseObject == null ? HttpStatusCode.NoContent : HttpStatusCode.OK);

    public IReadOnlyList<Uri> RequestUris
    {
        get
        {
            lock (_gate)
            {
                return [.. _requestUris];
            }
        }
    }

    public Uri LastRequestUri => RequestUris[^1];

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        lock (_gate)
        {
            _requestUris.Add(request.RequestUri!);
        }

        return Task.FromResult(
            new HttpResponseMessage
            {
                StatusCode = _statusCode,
                Content =
                    responseObject == null
                        ? new StringContent("", Encoding.UTF8)
                        : new StringContent(
                            JsonConvert.SerializeObject(responseObject),
                            Encoding.UTF8,
                            "application/json"
                        ),
            }
        );
    }
}

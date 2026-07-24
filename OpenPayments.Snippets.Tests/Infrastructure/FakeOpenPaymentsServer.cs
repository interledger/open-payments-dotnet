using System.Collections.ObjectModel;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Interledger.OpenPayments.Generated.Auth;
using Interledger.OpenPayments.Generated.Resource;
using Interledger.OpenPayments.Generated.Wallet;
using Interledger.OpenPayments.Serialization;
using Amount = Interledger.OpenPayments.Generated.Resource.Amount;

namespace OpenPayments.Snippets.Tests.Infrastructure;

/// <summary>
/// A minimal, stateful, in-process Open Payments server. Implements just enough of the
/// grant, incoming-payment, quote, and outgoing-payment protocol for the Guides in
/// OpenPayments.Snippets to run against it unmodified. One backend serves every
/// guide-visible wallet address, since every resource path (<c>/customer</c> vs
/// <c>/sender</c>, <c>/incoming-payments/{id}</c>, ...) is already unique.
/// </summary>
public sealed class FakeOpenPaymentsServer : IDisposable
{
    private readonly TestServer _testServer;

    private readonly Dictionary<string, WalletAddress> _walletAddresses = new();
    private readonly HashSet<string> _pendingGrantIds = new();
    private readonly Dictionary<string, string> _continuationTokenToGrantId = new();
    private readonly HashSet<string> _accessTokens = new();

    private readonly Dictionary<string, IncomingPaymentResponse> _incomingPayments = new();
    private readonly Dictionary<string, QuoteResponse> _quotes = new();
    private readonly Dictionary<string, OutgoingPaymentWithSpentAmountsResponse> _outgoingPayments = new();

    public IReadOnlyDictionary<string, IncomingPaymentResponse> IncomingPayments => _incomingPayments;
    public IReadOnlyDictionary<string, QuoteResponse> Quotes => _quotes;
    public IReadOnlyDictionary<string, OutgoingPaymentWithSpentAmountsResponse> OutgoingPayments => _outgoingPayments;
    public IReadOnlyCollection<string> IssuedAccessTokens => _accessTokens;

    public Uri BaseAddress => _testServer.BaseAddress;

    public FakeOpenPaymentsServer()
    {
        var hostBuilder = new WebHostBuilder()
            .ConfigureServices(services => services.AddRouting())
            .Configure(app =>
            {
                app.UseRouting();
                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapGet("/{*path}", HandleGetWalletAddress);
                    endpoints.MapPost("/", HandleCreateGrant);
                    endpoints.MapPost("/continue/{id}", HandleContinueGrant);
                    endpoints.MapPost("/incoming-payments", HandleCreateIncomingPayment);
                    endpoints.MapPost("/quotes", HandleCreateQuote);
                    endpoints.MapPost("/outgoing-payments", HandleCreateOutgoingPayment);
                });
            });

        _testServer = new TestServer(hostBuilder);
    }

    public HttpMessageHandler CreateHandler() => _testServer.CreateHandler();

    public void Dispose() => _testServer.Dispose();

    private static string NewId() => Guid.NewGuid().ToString("N");

    private static async Task<T> ReadBodyAsync<T>(HttpRequest request)
    {
        using var reader = new StreamReader(request.Body);
        var json = await reader.ReadToEndAsync();
        return JsonConvert.DeserializeObject<T>(json, OpenPaymentsSerialization.DefaultSettings)!;
    }

    private static async Task<JObject> ReadBodyAsJObjectAsync(HttpRequest request)
    {
        using var reader = new StreamReader(request.Body);
        var json = await reader.ReadToEndAsync();
        return JObject.Parse(json);
    }

    private static async Task WriteJsonAsync(HttpResponse response, int statusCode, object body)
    {
        response.StatusCode = statusCode;
        response.ContentType = "application/json";
        await response.WriteAsync(JsonConvert.SerializeObject(body, OpenPaymentsSerialization.DefaultSettings));
    }

    private static string? GetBearerToken(HttpRequest request)
    {
        var header = request.Headers["Authorization"].ToString();
        return header.StartsWith("GNAP ", StringComparison.Ordinal) ? header["GNAP ".Length..] : null;
    }

    private bool TryAuthorize(HttpRequest request)
    {
        var token = GetBearerToken(request);
        return token != null && _accessTokens.Contains(token);
    }

    private Task HandleGetWalletAddress(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "/";
        if (!_walletAddresses.TryGetValue(path, out var wallet))
        {
            wallet = new WalletAddress
            {
                Id = new Uri(BaseAddress, path),
                PublicName = path.Trim('/'),
                AssetCode = "USD",
                AssetScale = 2,
                AuthServer = BaseAddress,
                ResourceServer = BaseAddress,
            };
            _walletAddresses[path] = wallet;
        }

        return WriteJsonAsync(context.Response, StatusCodes.Status200OK, wallet);
    }

    // Non-interactive grants (Incoming, Quote) issue an AccessToken immediately.
    // Interactive grants (Outgoing, with `interact`) return a Continue URI instead.
    private async Task HandleCreateGrant(HttpContext context)
    {
        var body = await ReadBodyAsJObjectAsync(context.Request);

        if (body["interact"] is not null)
        {
            var grantId = NewId();
            var continuationToken = NewId();
            _pendingGrantIds.Add(grantId);
            _continuationTokenToGrantId[continuationToken] = grantId;

            await WriteJsonAsync(context.Response, StatusCodes.Status200OK, new AuthResponse
            {
                Continue = new AuthContinue
                {
                    AccessToken = new ContinueAccessToken { Value = continuationToken },
                    Uri = new Uri(BaseAddress, $"continue/{grantId}"),
                },
            });
            return;
        }

        var token = NewId();
        _accessTokens.Add(token);

        await WriteJsonAsync(context.Response, StatusCodes.Status200OK, new AuthResponse
        {
            AccessToken = new AccessTokenResponse { Value = token, Access = new Collection<AccessItem>() },
        });
    }

    // Auto-approves any interactRef (guides only ever pass a locally generated GUID),
    // simulating completed user consent, and issues the final resource access token.
    private async Task HandleContinueGrant(HttpContext context)
    {
        var id = (string)context.Request.RouteValues["id"]!;
        var token = GetBearerToken(context.Request);

        if (token is null || !_continuationTokenToGrantId.TryGetValue(token, out var grantId) || grantId != id)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var body = await ReadBodyAsync<GrantContinueBody>(context.Request);
        if (string.IsNullOrEmpty(body.InteractRef))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        _pendingGrantIds.Remove(grantId);
        _continuationTokenToGrantId.Remove(token);

        var accessToken = NewId();
        _accessTokens.Add(accessToken);

        await WriteJsonAsync(context.Response, StatusCodes.Status200OK, new AuthResponse
        {
            AccessToken = new AccessTokenResponse { Value = accessToken, Access = new Collection<AccessItem>() },
        });
    }

    private async Task HandleCreateIncomingPayment(HttpContext context)
    {
        if (!TryAuthorize(context.Request))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var body = await ReadBodyAsync<Interledger.OpenPayments.Generated.Resource.Body>(context.Request);
        var id = new Uri(BaseAddress, $"incoming-payments/{NewId()}");
        var receivedAmountCurrency = body.IncomingAmount ?? new Amount("0", "USD", 2);

        var response = new IncomingPaymentResponse
        {
            Id = id,
            WalletAddress = body.WalletAddress,
            Completed = false,
            IncomingAmount = body.IncomingAmount,
            ReceivedAmount = new Amount("0", receivedAmountCurrency.AssetCode, receivedAmountCurrency.AssetScale),
            ExpiresAt = body.ExpiresAt,
            Metadata = body.Metadata,
            CreatedAt = DateTimeOffset.UtcNow,
            Methods = new Collection<IlpPaymentMethod>
            {
                new()
                {
                    Type = IlpPaymentMethodType.Ilp,
                    IlpAddress = $"test.wallet.{NewId()}",
                    SharedSecret = Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
                },
            },
        };

        _incomingPayments[id.ToString()] = response;
        await WriteJsonAsync(context.Response, StatusCodes.Status201Created, response);
    }

    // A bare QuoteBody (no debitAmount/receiveAmount) quotes against the receiver
    // incoming payment's own incomingAmount — the "receiver is an Incoming Payment with
    // an incomingAmount" case every bare-QuoteBody guide relies on.
    private async Task HandleCreateQuote(HttpContext context)
    {
        if (!TryAuthorize(context.Request))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var raw = await ReadBodyAsJObjectAsync(context.Request);
        var walletAddress = (Uri)raw["walletAddress"]!;
        var receiver = (Uri)raw["receiver"]!;

        Amount debitAmount;
        Amount receiveAmount;

        if (raw["debitAmount"] is JObject debitJson)
        {
            debitAmount = debitJson.ToObject<Amount>()!;
            var receiverWallet = WalletForIncomingPayment(receiver);
            receiveAmount = new Amount(debitAmount.Value, receiverWallet.AssetCode, receiverWallet.AssetScale);
        }
        else if (raw["receiveAmount"] is JObject receiveJson)
        {
            receiveAmount = receiveJson.ToObject<Amount>()!;
            var senderWallet = _walletAddresses[walletAddress.AbsolutePath];
            debitAmount = new Amount(receiveAmount.Value, senderWallet.AssetCode, senderWallet.AssetScale);
        }
        else
        {
            var incomingPayment = _incomingPayments[receiver.ToString()];
            var amount = incomingPayment.IncomingAmount
                ?? throw new InvalidOperationException(
                    $"Quote receiver {receiver} has no incomingAmount to quote against."
                );
            debitAmount = amount;
            receiveAmount = amount;
        }

        var id = new Uri(BaseAddress, $"quotes/{NewId()}");
        var response = new QuoteResponse
        {
            Id = id,
            WalletAddress = walletAddress,
            Receiver = receiver,
            DebitAmount = debitAmount,
            ReceiveAmount = receiveAmount,
            Method = PaymentMethod.Ilp,
            ExpiresAt = "",
            CreatedAt = DateTimeOffset.UtcNow,
        };

        _quotes[id.ToString()] = response;
        await WriteJsonAsync(context.Response, StatusCodes.Status201Created, response);
    }

    private WalletAddress WalletForIncomingPayment(Uri receiver)
    {
        var incomingPayment = _incomingPayments[receiver.ToString()];
        return _walletAddresses[incomingPayment.WalletAddress.AbsolutePath];
    }

    private async Task HandleCreateOutgoingPayment(HttpContext context)
    {
        if (!TryAuthorize(context.Request))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var raw = await ReadBodyAsJObjectAsync(context.Request);
        var walletAddress = (Uri)raw["walletAddress"]!;

        OutgoingPaymentWithSpentAmountsResponse response;
        if (raw["quoteId"] is not null)
        {
            var quoteId = (Uri)raw["quoteId"]!;
            var quote = _quotes[quoteId.ToString()];
            response = new OutgoingPaymentWithSpentAmountsResponse
            {
                Id = new Uri(BaseAddress, $"outgoing-payments/{NewId()}"),
                WalletAddress = walletAddress,
                QuoteId = quote.Id,
                Receiver = quote.Receiver,
                DebitAmount = quote.DebitAmount,
                ReceiveAmount = quote.ReceiveAmount,
                SentAmount = new Amount("0", quote.DebitAmount.AssetCode, quote.DebitAmount.AssetScale),
                Failed = false,
                CreatedAt = DateTimeOffset.UtcNow,
                GrantSpentDebitAmount = quote.DebitAmount,
                GrantSpentReceiveAmount = quote.ReceiveAmount,
            };
        }
        else
        {
            var incomingPaymentId = (Uri)raw["incomingPayment"]!;
            var incomingPayment = _incomingPayments[incomingPaymentId.ToString()];
            var debitAmount = raw["debitAmount"]!.ToObject<Amount>()!;

            response = new OutgoingPaymentWithSpentAmountsResponse
            {
                Id = new Uri(BaseAddress, $"outgoing-payments/{NewId()}"),
                WalletAddress = walletAddress,
                QuoteId = null,
                Receiver = incomingPayment.Id,
                DebitAmount = debitAmount,
                ReceiveAmount = debitAmount,
                SentAmount = new Amount("0", debitAmount.AssetCode, debitAmount.AssetScale),
                Failed = false,
                CreatedAt = DateTimeOffset.UtcNow,
                GrantSpentDebitAmount = debitAmount,
                GrantSpentReceiveAmount = debitAmount,
            };
        }

        _outgoingPayments[response.Id.ToString()] = response;
        await WriteJsonAsync(context.Response, StatusCodes.Status201Created, response);
    }
}

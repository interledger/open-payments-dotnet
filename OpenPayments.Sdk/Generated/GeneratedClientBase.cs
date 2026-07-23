using System.Globalization;
using Newtonsoft.Json;

namespace Interledger.OpenPayments.Generated;

/// <summary>
/// Shared HTTP and serialization plumbing for the hand-written API client partial classes
/// (<c>AuthServerClient</c>, <c>ResourceServerClient</c>, <c>WalletAddressClient</c>).
/// Replaces the infrastructure NSwag used to emit into each <c>*.g.cs</c> file before the
/// switch to types-only generation (<c>/GenerateClientClasses:false</c>).
/// </summary>
public abstract class GeneratedClientBase
{
    protected readonly HttpClient _httpClient;

    protected GeneratedClientBase(HttpClient httpClient, JsonSerializerSettings serializerSettings)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        JsonSerializerSettings = serializerSettings;
    }

    /// <summary>Serializer settings used for request bodies and response parsing.</summary>
    protected JsonSerializerSettings JsonSerializerSettings { get; }

    /// <summary>
    /// When <c>true</c>, response bodies are buffered as strings before deserialization
    /// (and included in exception details); otherwise they are streamed.
    /// </summary>
    public bool ReadResponseAsString { get; set; }

    protected readonly struct ObjectResponseResult<T>(T responseObject, string responseText)
    {
        public T Object { get; } = responseObject;

        public string Text { get; } = responseText;
    }

    protected static Task<string> ReadAsStringAsync(
        HttpContent? content,
        CancellationToken cancellationToken
    ) => content == null ? Task.FromResult(string.Empty) : content.ReadAsStringAsync(cancellationToken);

    protected virtual async Task<ObjectResponseResult<T>> ReadObjectResponseAsync<T>(
        HttpResponseMessage? response,
        IReadOnlyDictionary<string, IEnumerable<string>> headers,
        CancellationToken cancellationToken
    )
    {
        if (response == null || response.Content == null)
        {
            return new ObjectResponseResult<T>(default!, string.Empty);
        }

        if (ReadResponseAsString)
        {
            var responseText = await ReadAsStringAsync(response.Content, cancellationToken)
                .ConfigureAwait(false);
            try
            {
                var typedBody = JsonConvert.DeserializeObject<T>(responseText, JsonSerializerSettings);
                return new ObjectResponseResult<T>(typedBody!, responseText);
            }
            catch (JsonException exception)
            {
                throw OpenPaymentsExceptionFactory.Create(
                    "Could not deserialize the response body string as " + typeof(T).FullName + ".",
                    (int)response.StatusCode,
                    null,
                    responseText,
                    headers,
                    exception
                );
            }
        }

        try
        {
            using var responseStream = await response
                .Content.ReadAsStreamAsync(cancellationToken)
                .ConfigureAwait(false);
            using var streamReader = new StreamReader(responseStream);
            using var jsonTextReader = new JsonTextReader(streamReader);
            var serializer = JsonSerializer.Create(JsonSerializerSettings);
            var typedBody = serializer.Deserialize<T>(jsonTextReader);
            return new ObjectResponseResult<T>(typedBody!, string.Empty);
        }
        catch (JsonException exception)
        {
            throw OpenPaymentsExceptionFactory.Create(
                "Could not deserialize the response body stream as " + typeof(T).FullName + ".",
                (int)response.StatusCode,
                null,
                string.Empty,
                headers,
                exception
            );
        }
    }

    protected static string ConvertToString(object? value, CultureInfo cultureInfo)
    {
        switch (value)
        {
            case null:
                return string.Empty;

            case Enum:
            {
                var name = Enum.GetName(value.GetType(), value);
                if (name != null)
                {
                    var field = value.GetType().GetField(name);
                    if (
                        field != null
                        && Attribute.GetCustomAttribute(
                            field,
                            typeof(System.Runtime.Serialization.EnumMemberAttribute)
                        )
                            is System.Runtime.Serialization.EnumMemberAttribute attribute
                    )
                    {
                        return attribute.Value ?? name;
                    }

                    return Convert.ToString(
                            Convert.ChangeType(
                                value,
                                Enum.GetUnderlyingType(value.GetType()),
                                cultureInfo
                            )
                        ) ?? string.Empty;
                }

                break;
            }

            case bool flag:
                return Convert.ToString(flag, cultureInfo).ToLowerInvariant();

            case byte[] bytes:
                return Convert.ToBase64String(bytes);

            case string[] strings:
                return string.Join(",", strings);

            case Array array:
            {
                var items = new List<string>();
                foreach (var item in array)
                    items.Add(ConvertToString(item, cultureInfo));
                return string.Join(",", items);
            }
        }

        return Convert.ToString(value, cultureInfo) ?? string.Empty;
    }

    protected static string NormalizeBaseUrl(Uri baseUri)
    {
        var value = baseUri.ToString();
        return value.EndsWith("/") ? value : value + "/";
    }
}

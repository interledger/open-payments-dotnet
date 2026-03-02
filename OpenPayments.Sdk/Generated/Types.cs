namespace OpenPayments.Sdk.Generated
{
    public abstract partial class Anonymous
    {
        private IDictionary<string, object>? _additionalProperties;

        [Newtonsoft.Json.JsonExtensionData]
        public IDictionary<string, object> AdditionalProperties
        {
            get { return _additionalProperties ??= new Dictionary<string, object>(); }
            set => _additionalProperties = value;
        }
    }

    public static class Helpers
    {
        public static string StatusAsText(int status)
        {
            return status switch
            {
                400 => "Bad Request",
                401 => "Unauthorized",
                403 => "Forbidden",
                404 => "Not Found",
                500 => "Internal Server Error",
                _ => "Unknown Status Code",
            };
        }

        public static Dictionary<string, IEnumerable<string>> ExtractHeaders(
            HttpResponseMessage response
        )
        {
            var headers = new Dictionary<string, IEnumerable<string>>();
            foreach (var item in response.Headers)
                headers[item.Key] = item.Value;
            foreach (var item in response.Content.Headers)
                headers[item.Key] = item.Value;

            return headers;
        }
    }
}

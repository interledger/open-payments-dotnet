namespace OpenPayments.Sdk.Generated
{
    /// <summary>
    /// Base class shared by generated models that captures any JSON properties present on the wire but not
    /// mapped to a declared member.
    /// </summary>
    public abstract partial class Anonymous
    {
        private IDictionary<string, object>? _additionalProperties;

        /// <summary>
        /// JSON properties present on the response but not declared on the generated type.
        /// </summary>
        [Newtonsoft.Json.JsonExtensionData]
        public IDictionary<string, object> AdditionalProperties
        {
            get { return _additionalProperties ??= new Dictionary<string, object>(); }
            set => _additionalProperties = value;
        }
    }

    /// <summary>
    /// Helper utilities shared by the generated Auth, Resource, and Wallet clients.
    /// </summary>
    public static class Helpers
    {
        /// <summary>
        /// Combines an <see cref="HttpResponseMessage"/>'s response headers and content headers into a
        /// single dictionary.
        /// </summary>
        /// <param name="response">The HTTP response to extract headers from.</param>
        /// <returns>A dictionary mapping header name to its values.</returns>
        public static Dictionary<string, IEnumerable<string>> ExtractHeaders(HttpResponseMessage response)
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

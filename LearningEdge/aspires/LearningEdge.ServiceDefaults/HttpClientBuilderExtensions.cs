using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.DependencyInjection
{
    // Minimal default implementation so projects referencing ServiceDefaults compile
    // and can optionally provide a more complete resilience setup elsewhere.
    public static class HttpClientBuilderExtensions
    {
        public static IHttpClientBuilder AddStandardResilienceHandler(this IHttpClientBuilder builder)
        {
            // Default no-op: keep the IHttpClientBuilder fluent API usable.
            return builder;
        }
    }
}

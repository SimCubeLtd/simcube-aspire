namespace SimCube.Aspire.Hosting.Features.Otlp;

public static class OtlpResourceExtensions
{
    extension<T>(IResourceBuilder<T> builder) where T : IResourceWithEnvironment
    {
        public IResourceBuilder<T> WithServiceName(string name) =>
            builder.WithEnvironment(OtlpLiterals.ServiceName, name);

        public IResourceBuilder<T> WithOtlpEndpoint(string uri) =>
            builder.WithEnvironment(OtlpLiterals.Endpoint, uri);

        public IResourceBuilder<T> WithOtlpEndpoint(Uri uri) =>
            builder.WithOtlpEndpoint(uri.ToString());
    }
}

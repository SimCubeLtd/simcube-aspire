namespace SimCube.Aspire.Hosting.Features.Otlp;

public static class OtlpResourceExtensions
{
    public static IResourceBuilder<T> WithServiceName<T>(this IResourceBuilder<T> builder, string name) where T : IResourceWithEnvironment =>
        builder.WithEnvironment(OtlpLiterals.ServiceName, name);

    public static IResourceBuilder<T> WithOtlpEndpoint<T>(this IResourceBuilder<T> builder, string uri) where T : IResourceWithEnvironment =>
        builder.WithEnvironment(OtlpLiterals.Endpoint, uri);

    public static IResourceBuilder<T> WithOtlpEndpoint<T>(this IResourceBuilder<T> builder, Uri uri) where T : IResourceWithEnvironment =>
        builder.WithOtlpEndpoint(uri.ToString());
}

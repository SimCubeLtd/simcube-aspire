namespace SimCube.Aspire.Hosting.Features.Seq;

public static class SeqExtensions
{
    public static IResourceBuilder<T> WithSeqEndpointFromResource<T>(this IResourceBuilder<T> builder, IResourceBuilder<SeqResource> seqResource) where T : IResourceWithEnvironment
    {
        var endpointReference = seqResource.GetEndpoint("http");

        var host = endpointReference.Property(EndpointProperty.Host);
        var port = endpointReference.Property(EndpointProperty.Port);

        return builder
            .WithEnvironment(SeqLiterals.SeqEndpoint, $"http://{host}:{port}");
    }
}

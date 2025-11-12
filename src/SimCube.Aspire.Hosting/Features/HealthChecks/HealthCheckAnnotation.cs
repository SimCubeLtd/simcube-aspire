namespace SimCube.Aspire.Hosting.Features.HealthChecks;

public class HealthCheckAnnotation(Func<IResource, CancellationToken, Task<IHealthCheck?>> healthCheckFactory) : IResourceAnnotation
{
    /// <summary>
    /// A factory that creates a health check from a resource
    /// </summary>
    public Func<IResource, CancellationToken, Task<IHealthCheck?>> HealthCheckFactory { get; } = healthCheckFactory;

    /// <summary>
    /// Create a <see cref="HealthCheckAnnotation"/> from 
    /// </summary>
    /// <param name="connectionStringFactory"></param>
    /// <returns>A new <see cref="HealthCheckAnnotation"/>.</returns>
    public static HealthCheckAnnotation Create(Func<string, IHealthCheck> connectionStringFactory) =>
        new(async (resource, token) => resource is not IResourceWithConnectionString c
            ?  null
            : await c.GetConnectionStringAsync(token) is not { } cs ? null : connectionStringFactory(cs));
}
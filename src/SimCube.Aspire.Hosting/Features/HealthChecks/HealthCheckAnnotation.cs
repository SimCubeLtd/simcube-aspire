namespace SimCube.Aspire.Hosting.Features.HealthChecks;

/*
 * MIT License

   Copyright (c) 2024 David Fowler

   Permission is hereby granted, free of charge, to any person obtaining a copy
   of this software and associated documentation files (the "Software"), to deal
   in the Software without restriction, including without limitation the rights
   to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
   copies of the Software, and to permit persons to whom the Software is
   furnished to do so, subject to the following conditions:

   The above copyright notice and this permission notice shall be included in all
   copies or substantial portions of the Software.

   THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
   IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
   FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
   AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
   LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
   OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
   SOFTWARE.
 */
//todo: Remove when David Fowler publishes utilities nuget package, and reference that.
public class HealthCheckAnnotation(Func<IResource, CancellationToken, Task<IHealthCheck?>> healthCheckFactory) : IResourceAnnotation
{
    public Func<IResource, CancellationToken, Task<IHealthCheck?>> HealthCheckFactory { get; } = healthCheckFactory;

    public static HealthCheckAnnotation Create(Func<string, IHealthCheck> connectionStringFactory) =>
        new(
            async (resource, token) =>
            {
                if (resource is not IResourceWithConnectionString resourceWithConnectionString)
                {
                    Console.WriteLine($"[{nameof(HealthCheckAnnotation)}] `{resource.Name}` : Resource does not have a connection string");
                    return null;
                }

                var cs = await resourceWithConnectionString.GetConnectionStringAsync(token);

                if (cs == null)
                {
                    Console.WriteLine($"[{nameof(HealthCheckAnnotation)}] `{resource.Name}` : Connection string is null");
                    return null;
                }

                return connectionStringFactory(cs);
            });
}

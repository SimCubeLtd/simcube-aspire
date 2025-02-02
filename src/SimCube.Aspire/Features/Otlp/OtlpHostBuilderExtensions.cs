namespace SimCube.Aspire.Features.Otlp;

public static class OtlpHostBuilderExtensions
{
    public static void AddOtlpServiceDefaults(
        this IHostBuilder builder,
        string consoleOutputFormat = OtlpLoggingExtensions.ConsoleOutputFormat,
        bool lokiCompatible = false,
        bool rawCompactJson = false)
    {
        if (string.IsNullOrEmpty(consoleOutputFormat))
        {
            consoleOutputFormat = OtlpLoggingExtensions.ConsoleOutputFormat;
        }
        
        builder.ConfigureLogging((ctx, loggingBuilder) => ConfigureLogging(ctx, loggingBuilder, consoleOutputFormat, lokiCompatible, rawCompactJson));
        
        builder.ConfigureServices(ConfigureServices);
    }
    
    public static void MapHealthcheckEndpoints(this WebApplication app)
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("OTEL_EXPOSE_HEALTHCHECKS")))
        {
            return;
        }

        app.MapHealthChecks("/health");

        app.MapHealthChecks("/alive", new()
        {
            Predicate = r => r.Tags.Contains("live"),
        });
    }

    private static void ConfigureServices(HostBuilderContext ctx, IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);
        
        services.AddServiceDiscovery();

        services.ConfigureHttpClientDefaults(http =>
        {
            http.AddStandardResilienceHandler();
            http.AddServiceDiscovery();
        });

        if (!string.IsNullOrEmpty(ctx.Configuration[OtlpLiterals.Endpoint]))
        {
               services.AddOpenTelemetry()
                .WithMetrics(
                    metrics =>
                    {
                        metrics.AddAspNetCoreInstrumentation()
                            .AddHttpClientInstrumentation()
                            .AddRuntimeInstrumentation();
                    })
                .WithTracing(
                    tracing =>
                    {
                        tracing.AddAspNetCoreInstrumentation()
                            .AddHttpClientInstrumentation(
                                options => options.FilterHttpRequestMessage = (HttpRequestMessage request) =>
                                    !request.RequestUri?.AbsoluteUri.Contains(
                                        ctx.Configuration[OtlpLiterals.Endpoint],
                                        StringComparison.Ordinal) ?? true);
                    });

            var useOtlpExporter = !string.IsNullOrWhiteSpace(ctx.Configuration[OtlpLiterals.Endpoint]);

            if (useOtlpExporter)
            {
                services.AddOpenTelemetry().UseOtlpExporter();
            }
        }
    }

    private static void ConfigureLogging(HostBuilderContext ctx,
        ILoggingBuilder loggingBuilder,
        string consoleOutputFormat,
        bool lokiCompatible,
        bool rawCompactJson)
    {
        loggingBuilder.ClearProviders();

        var logger = ctx.Configuration.GetLoggerConfiguration(consoleOutputFormat, lokiCompatible, rawCompactJson).CreateLogger();
        
        loggingBuilder.AddSerilog(logger, true);
        
        if (string.IsNullOrEmpty(ctx.Configuration[OtlpLiterals.Endpoint]))
        {
            return;
        }
        
        loggingBuilder.AddOpenTelemetry(
            logging =>
            {
                logging.IncludeFormattedMessage = true;
                logging.IncludeScopes = true;
            });
    }
}
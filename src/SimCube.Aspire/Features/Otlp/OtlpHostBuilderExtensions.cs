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
        
        builder.ConfigureServices((ctx, services) => ConfigureOtlpServices(ctx.Configuration, services));
        builder.ConfigureServices(ConfigureHttpClientWithServiceDiscovery);
    }
    
    public static void ConfigureOtlpLogging(ILoggingBuilder loggingBuilder) =>
        loggingBuilder.AddOpenTelemetry(
            logging =>
            {
                logging.IncludeFormattedMessage = true;
                logging.IncludeScopes = true;
            });
    
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

    public static void ConfigureOtlpServices(IConfiguration configuration, IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        if (!string.IsNullOrEmpty(configuration[OtlpLiterals.Endpoint]))
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
                                        configuration[OtlpLiterals.Endpoint],
                                        StringComparison.Ordinal) ?? true);
                    });

            var useOtlpExporter = !string.IsNullOrWhiteSpace(configuration[OtlpLiterals.Endpoint]);

            if (useOtlpExporter)
            {
                services.AddOpenTelemetry().UseOtlpExporter();
            }
        }
    }

    public static void ConfigureHttpClientWithServiceDiscovery(IServiceCollection services)
    {
        services.AddServiceDiscovery();

        services.ConfigureHttpClientDefaults(http =>
        {
            http.AddStandardResilienceHandler();
            http.AddServiceDiscovery();
        });
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
        
        ConfigureOtlpLogging(loggingBuilder);
    }
}
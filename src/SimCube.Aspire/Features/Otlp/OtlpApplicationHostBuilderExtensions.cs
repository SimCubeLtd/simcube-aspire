using OpenTelemetry.Logs;

namespace SimCube.Aspire.Features.Otlp;

public static class OtlpApplicationHostBuilderExtensions
{
    public static void AddOtlpServiceDefaults(
        this IHostApplicationBuilder builder,
        string consoleOutputFormat = OtlpLoggingExtensions.ConsoleOutputFormat,
        bool lokiCompatible = false,
        bool rawCompactJson = false,
        Action<OpenTelemetryLoggerOptions>? configureLogger = null,
        Action<MeterProviderBuilder>? configureMeters = null, 
        Action<TracerProviderBuilder>? configureTracer = null)
    {
        if (string.IsNullOrEmpty(consoleOutputFormat))
        {
            consoleOutputFormat = OtlpLoggingExtensions.ConsoleOutputFormat;
        }
        
        builder.ConfigureSerilog(consoleOutputFormat, lokiCompatible, rawCompactJson);

        builder.ConfigureOpenTelemetry(configureLogger, configureMeters, configureTracer);

        builder.AddDefaultHealthChecks();

        builder.Services.AddServiceDiscovery();

        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            http.AddStandardResilienceHandler();
            http.AddServiceDiscovery();
        });
    }

    
    
    public static void MapDefaultEndpoints(this WebApplication app)
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

    private static void ConfigureSerilog(this IHostApplicationBuilder builder, string consoleOutputFormat, bool lokiCompatible, bool rawCompactJson)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddSerilog(builder.Configuration.GetLoggerConfiguration(consoleOutputFormat, lokiCompatible, rawCompactJson).CreateLogger(), true);
    }

    private static void ConfigureOpenTelemetryLogging(this IHostApplicationBuilder builder, Action<OpenTelemetryLoggerOptions>? configure = null)
    {
        if (configure is not null)
        {
            builder.Logging.AddOpenTelemetry(configure);
            return;
        }
        
        builder.Logging.AddOpenTelemetry(
            logging =>
            {
                logging.IncludeFormattedMessage = true;
                logging.IncludeScopes = true;
            });
    }

    private static void ConfigureOpenTelemetry(
        this IHostApplicationBuilder builder, 
        Action<OpenTelemetryLoggerOptions>? configureLogger = null,
        Action<MeterProviderBuilder>? configureMeters = null, 
        Action<TracerProviderBuilder>? configureTracer = null)
    {
        if (string.IsNullOrEmpty(builder.Configuration[OtlpLiterals.Endpoint]))
        {
            return;
        }

        builder.ConfigureOpenTelemetryLogging(configureLogger);

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics.AddDefaultMetricConfiguration();
                configureMeters?.Invoke(metrics);
            })
            .WithTracing(tracing =>
            {
                tracing.AddDefaultTracing(builder.Configuration);
                configureTracer?.Invoke(tracing);
            });

        builder.AddOpenTelemetryExporters();
    }

    private static void AddOpenTelemetryExporters(this IHostApplicationBuilder builder)
    {
        var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration[OtlpLiterals.Endpoint]);

        if (useOtlpExporter)
        {
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }
    }

    private static void AddDefaultHealthChecks(this IHostApplicationBuilder builder) =>
        builder.Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);
    
    private static void AddDefaultTracing(this TracerProviderBuilder tracing, IConfiguration configuration) =>
        tracing.AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation(options => options.FilterHttpRequestMessage = (HttpRequestMessage request) =>
                !request.RequestUri?.AbsoluteUri.Contains(configuration[OtlpLiterals.Endpoint],
                    StringComparison.Ordinal) ?? true);

    private static void AddDefaultMetricConfiguration(this MeterProviderBuilder metrics) =>
        metrics.AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation();
}
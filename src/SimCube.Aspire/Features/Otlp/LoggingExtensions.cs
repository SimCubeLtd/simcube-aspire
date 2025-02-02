namespace SimCube.Aspire.Features.Otlp;

public static class OtlpLoggingExtensions
{
    public const string ConsoleOutputFormat = "[{Timestamp:HH:mm:ss}] | {Level:u4} | {SourceContext} | {Message:lj}{NewLine}{Exception}";
    
    public static LoggerConfiguration GetLoggerConfiguration(
        this IConfiguration configuration,
        string consoleOutputFormat = ConsoleOutputFormat,
        bool lokiCompatible = false,
        bool rawCompactJson = false)
    {
        var config = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithProcessId()
            .Enrich.WithProcessName()
            .Enrich.WithThreadId()
            .Enrich.WithSpan()
            .Enrich.WithExceptionDetails(new DestructuringOptionsBuilder()
                .WithDefaultDestructurers())
            .Enrich.WithProperty(nameof(OtlpLiterals.ServiceName), configuration[OtlpLiterals.ServiceName]);

        config = lokiCompatible switch
        {
            true => config.WriteTo.Console(rawCompactJson ? new RenderedCompactJsonFormatter() : new LokiJsonTextFormatter()),
            false => config.WriteTo.Spectre(outputTemplate: consoleOutputFormat),
        };
        
        var otlpEndpoint = configuration.GetValue(OtlpLiterals.Endpoint, string.Empty);
        
        if (!string.IsNullOrEmpty(otlpEndpoint))
        {
            config = config.WriteTo.OpenTelemetry(options =>
            {
                options.IncludedData = IncludedData.TraceIdField | IncludedData.SpanIdField;
                options.Endpoint = configuration[OtlpLiterals.Endpoint];
                AddHeaders(options.Headers, configuration[OtlpLiterals.Headers]);
                AddResourceAttributes(options.ResourceAttributes, configuration[OtlpLiterals.ResourceAttributes]);
                options.ResourceAttributes.Add("service.name", configuration[OtlpLiterals.ServiceName]);
            });
        }

        var seqEndpoint = configuration.GetValue(SeqLiterals.SeqEndpoint, string.Empty);
        
        if (!string.IsNullOrEmpty(seqEndpoint))
        {
            var apiKey = configuration.GetValue(SeqLiterals.SeqApiKey, string.Empty);
            config = config.WriteTo.Seq(serverUrl: seqEndpoint, apiKey: apiKey);
        }

        return config;
    }
    
    private static void AddResourceAttributes(IDictionary<string, object> attributes, string attributeConfig)
    {
        if (!string.IsNullOrEmpty(attributeConfig))
        {
            var parts = attributeConfig.Split('=');

            if (parts.Length == 2)
            {
                attributes[parts[0]] = parts[1];
                return;
            }

            throw new InvalidOperationException($"Invalid resource attribute format: {attributeConfig}");
        }
    }

    private static void AddHeaders(IDictionary<string, string> headers, string headerConfig)
    {
        if (!string.IsNullOrEmpty(headerConfig))
        {
            foreach (var header in headerConfig.Split(','))
            {
                var parts = header.Split('=');

                if (parts.Length == 2)
                {
                    headers[parts[0]] = parts[1];
                    continue;
                }

                throw new InvalidOperationException($"Invalid header format: {header}");
            }
        }
    }
}
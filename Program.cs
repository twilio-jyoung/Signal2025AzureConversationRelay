using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Signal2025AzureConversationRelay.Services;
using Signal2025AzureConversationRelay.Middleware;
using System.Linq;
using System;
using Signal2025AzureConversationRelay;
using Signal2025AzureConversationRelay.Functions.Triggers.Twilio;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Logging.ClearProviders();
builder.Logging.AddColorConsoleLogger();
builder.Logging.AddApplicationInsights();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();
builder.Logging.SetMinimumLevel(LogLevel.Trace);

builder.Logging.AddFilter("Microsoft", LogLevel.Warning);
builder.Logging.AddFilter("System", LogLevel.Warning);
builder.Logging.AddFilter("Azure", LogLevel.Warning);
builder.Logging.AddFilter("Worker", LogLevel.None);
builder.Logging.AddFilter("Signal2025AzureConversationRelay", LogLevel.Trace);
builder.Logging.AddFilter("Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider", LogLevel.Debug);
builder.Logging.AddFilter("Microsoft.Extensions.Logging.Console.ConsoleLoggerProvider", LogLevel.Debug);
builder.Logging.Services.Configure<LoggerFilterOptions>(options =>
{
    var defaultRule = options.Rules.FirstOrDefault(rule => rule.ProviderName
        == "Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider");
    if (defaultRule is not null)
    {
        options.Rules.Remove(defaultRule);
    }
});

// validate Twilio webhook requests
builder.UseWhen<ValidateTwilioRequestMiddleware>((context) => {

    var shouldValidateTwilioSignature = 
        context.FunctionDefinition.Name.Equals(nameof(IncomingCallHttpTriggerFunction), StringComparison.OrdinalIgnoreCase) 
        ||
        context.FunctionDefinition.Name.Equals(nameof(CallStatusHttpTriggerFunction), StringComparison.OrdinalIgnoreCase);

    return shouldValidateTwilioSignature;
});

builder.Services.AddSingleton<TwiMLGeneratorService>();
builder.Services.AddSingleton<AzureWebPubSubService>();
builder.Services.AddSingleton<SemanticKernelService>();
builder.Services.AddSingleton<ColorConsoleLoggerConfiguration>();
builder.Services.AddSingleton<Func<ColorConsoleLoggerConfiguration>>(sp =>
{
    return () => sp.GetRequiredService<ColorConsoleLoggerConfiguration>();
});

builder.Configuration.AddJsonFile("local.settings.json", optional: true, reloadOnChange: true);
var host = builder.Build();

var logger = host.Services.GetRequiredService<ILogger<Program>>();

logger.LogTrace("This is a trace message");
logger.LogDebug("This is a debug message");
logger.LogInformation("This is an information message");
logger.LogWarning("This is a warning message");
logger.LogError("This is an error message");
logger.LogCritical("This is a critical message");

host.Run();
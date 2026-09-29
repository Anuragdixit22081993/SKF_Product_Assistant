using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.SemanticKernel;
using SKF_Product_Assistant.Services;
using Microsoft.Azure.Functions.Worker.OpenTelemetry;
using SKF_Product_Assistant.Agents;


var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

if (!string.IsNullOrEmpty(
    Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING")))
{
    builder.Services
        .AddOpenTelemetry()
        .UseFunctionsWorkerDefaults()
        .UseAzureMonitorExporter();
}

builder.Services.AddSingleton<ProductDataService>();
builder.Services.AddSingleton<ConversationStateService>();
builder.Services.AddSingleton<RedisConnectionService>();
builder.Services.AddSingleton<QaAgent>();
builder.Services.AddSingleton<FeedbackAgent>();
builder.Services.AddSingleton<ProductAssistantOrchestrator>();
builder.Services.AddSingleton<FeedbackStore>();

builder.Services.AddSingleton<Kernel>(serviceProvider =>
{
    var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
    var apiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY");
    var deploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT");

    if (string.IsNullOrWhiteSpace(endpoint))
    {
        throw new InvalidOperationException(
            "AZURE_OPENAI_ENDPOINT is not configured.");
    }

    if (string.IsNullOrWhiteSpace(apiKey))
    {
        throw new InvalidOperationException(
            "AZURE_OPENAI_API_KEY is not configured.");
    }

    if (string.IsNullOrWhiteSpace(deploymentName))
    {
        throw new InvalidOperationException(
            "AZURE_OPENAI_DEPLOYMENT is not configured.");
    }

    var kernelBuilder = Kernel.CreateBuilder();

    kernelBuilder.AddAzureOpenAIChatCompletion(
        deploymentName: deploymentName,
        endpoint: endpoint,
        apiKey: apiKey);

    return kernelBuilder.Build();
});

builder.Build().Run();
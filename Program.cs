using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using FuncSbProxy;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

// Register HttpClient and HttpMessageSender
builder.Services.AddHttpClient();
builder.Services.AddScoped<HttpMessageSender>();
builder.Services.AddScoped<DisableFuncMessenger>();

builder.Build().Run();

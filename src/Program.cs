using Contoso.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services.AddSingleton<IGraphService, GraphService>();
builder.Services.AddSingleton<IMonitoringService, MonitoringService>();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .AddHttpClient()
    .ConfigureFunctionsApplicationInsights();

builder.Build().Run();

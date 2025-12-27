using OpenTelemetry.NET.Extensions;
using Serilog;
using WebShop.Api.Extensions.Core;
using WebShop.Api.Extensions.Features;
using WebShop.Api.Extensions.Middleware;

try
{
    Log.Information("Starting WebShop API");

    WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

    // Initialize configuration providers in explicit order
    builder.InitializeConfigurationProviders();

    // Configure request size limits at Kestrel level (before services are built)
    builder.ConfigureKestrelRequestLimits();

    // Add OpenTelemetry observability (tracing, metrics, logging)
    builder.AddObservability();

    // Configure all API services
    builder.Services.ConfigureApiServices(builder.Configuration, builder.Environment);

    WebApplication app = builder.Build();

    // Configure middleware pipeline
    app.ConfigureMiddleware();

    // Configure health check endpoints
    app.ConfigureHealthCheckEndpoints();

    // Configure OpenAPI endpoints and Scalar UI
    app.ConfigureOpenApiEndpoints();

    // Start the web server and begin accepting HTTP requests
    await app.StartAsync();

    Log.Information("WebShop API started successfully");

    // Keep the application running until shutdown signal is received
    await app.WaitForShutdownAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "WebShop API terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

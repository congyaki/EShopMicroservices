using BuildingBlocks.Extensions;
using Catalog.API.Data;
using Catalog.API.Metrics;
using FluentValidation;
using HealthChecks.UI.Client;
using Marten;
using MediatR;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using BuildingBlocks.Services;
using Prometheus;
using Microsoft.AspNetCore.Builder;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

var assembly = typeof(Program).Assembly;

builder.Services.AddMediatR(config =>
{
    config.RegisterServicesFromAssembly(assembly);
    config.AddOpenBehavior(typeof(ValidationBehavior<,>));
    config.AddOpenBehavior(typeof(LoggingBehavior<,>));
});
builder.Services.AddValidatorsFromAssembly(assembly);

builder.Services.AddAutoMapper(assembly);

builder.Services.AddMarten(opts =>
{
    opts.Connection(builder.Configuration.GetConnectionString("Database")!);
}).UseLightweightSessions();

// Add CORS policy to allow Prometheus to scrape metrics
builder.Services.AddCors(options =>
{
    options.AddPolicy("MetricsPolicy", corsBuilder =>
    {
        corsBuilder
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader()
            .WithExposedHeaders("Content-Type");
    });
});

// Add Prometheus monitoring
builder.Services.AddPrometheusMonitoring("catalog-service");

// Add metrics background service
builder.Services.AddHostedService<CatalogMetricsHostedService>();

// Add custom metrics for Catalog service
builder.Services.AddCustomMetrics(metrics =>
{
    // Add any additional custom metrics that aren't already defined in CatalogMetrics
    metrics.AddCounter(
        "catalog_api_errors_total",
        "Total number of API errors in Catalog service",
        "error_type", "endpoint");
});

// Đăng ký và cấu hình CatalogSeeding options - chỉ trong môi trường Development
if (builder.Environment.IsDevelopment())
{
    builder.Services.Configure<CatalogSeedingOptions>(options =>
    {
        builder.Configuration.GetSection("CatalogSeedingOptions").Bind(options);
        // Cấu hình mặc định nếu không có trong appsettings
        if (options.BatchSize <= 0) options.BatchSize = 200;
        if (options.DelayBetweenBatchesMs <= 0) options.DelayBetweenBatchesMs = 100;
        if (options.StartupDelaySeconds <= 0) options.StartupDelaySeconds = 5;
    });

    // Đăng ký Background Service để seed data
    builder.Services.AddHostedService<CatalogDataSeedingService>();
    
    // Chỉ sử dụng Leader Election trong môi trường Development
    builder.Services.AddLeaderElection(builder.Configuration, builder.Environment);
    
    // Register ServiceDiscoveryHostedService - chỉ trong môi trường Development
    builder.Services.AddHostedService<ServiceDiscoveryHostedService>();
    
    Console.WriteLine("Leader Election và Service Discovery đã được đăng ký trong môi trường Development");
}
else
{
    // Trong môi trường Production, sử dụng NoOpLeaderElectionService để tránh phụ thuộc vào Consul
    builder.Services.AddSingleton<ILeaderElectionService>(sp =>
    {
        var logger = sp.GetRequiredService<ILogger<NoOpLeaderElectionService>>();
        return new NoOpLeaderElectionService(logger);
    });
    
    Console.WriteLine("NoOpLeaderElectionService đã được đăng ký trong môi trường Production");
}

// Đăng ký CatalogInitialData cho seed data ban đầu
builder.Services.AddScoped<CatalogInitialData>();

// Enhanced health checks with Prometheus metrics
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("Database")!)
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy())
    .ForwardToPrometheus();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
// Important: Order matters for middleware!

// Đầu tiên, sử dụng CORS để cho phép Prometheus scrape metrics
app.UseCors("MetricsPolicy");

// Sử dụng exception handler
app.UseMiddleware<CustomExceptionHandler>();

// Kích hoạt routing sớm trong pipeline
app.UseRouting();

// Đăng ký Prometheus HTTP metrics middleware
app.UseHttpMetrics();

// Authorize và Authentication (nếu cần)
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    // Safe database migration with leader election
    app.MigrateDatabaseSafely(async serviceProvider =>
    {
        var store = serviceProvider.GetRequiredService<IDocumentStore>();
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

        logger.LogInformation("Starting database schema migration");

        // Apply database schema changes
        await store.Storage.ApplyAllConfiguredChangesToDatabaseAsync();

        // If in development, seed initial data (chỉ seed một số sản phẩm mẫu ban đầu)
        if (app.Environment.IsDevelopment())
        {
            var initialData = serviceProvider.GetRequiredService<CatalogInitialData>();
            await initialData.InitializeAsync(serviceProvider);
            logger.LogInformation("Database seeded with initial sample data");
        }

        logger.LogInformation("Database migration completed");
    });
}

// Configure health checks với UI và metrics
app.UseHealthChecks("/health",
    new HealthCheckOptions
    {
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
    });

// Đăng ký endpoints (bao gồm controllers và metrics)
app.UseEndpoints(endpoints =>
{
    // Đăng ký controller endpoints
    endpoints.MapControllers();
    
    // Đảm bảo metrics endpoint luôn được đăng ký đúng cách
    endpoints.MapMetrics("/metrics").AllowAnonymous();
    
    // Thêm endpoint kiểm tra health của metrics
    endpoints.MapGet("/metrics-probe", async context =>
    {
        context.Response.StatusCode = 200;
        await context.Response.WriteAsync("Metrics endpoint is working!");
    });
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Initialize metrics that require service data
CatalogMetrics.InitializeMetrics(app.Services);

app.Run();

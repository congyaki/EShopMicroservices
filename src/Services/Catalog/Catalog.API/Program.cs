using BuildingBlocks.Extensions;
using Catalog.API.Data;
using FluentValidation;
using HealthChecks.UI.Client;
using Marten;
using MediatR;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using BuildingBlocks.Services;
using Consul;

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

// Register Consul client
builder.Services.AddSingleton<IConsulClient>(p => new ConsulClient(consulConfig =>
{
    var serviceConfig = builder.Configuration.GetServiceConfig();
    consulConfig.Address = new Uri($"http://{serviceConfig.ConsulHost}:{serviceConfig.ConsulPort}");
}));

// Register ServiceDiscoveryHostedService
builder.Services.AddHostedService<ServiceDiscoveryHostedService>();

builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("Database")!);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseMiddleware<CustomExceptionHandler>();

// Safe database migration with leader election
app.MigrateDatabaseSafely(async serviceProvider =>
{
    var store = serviceProvider.GetRequiredService<IDocumentStore>();
    var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
    
    logger.LogInformation("Starting database schema migration");
    
    // Apply database schema changes
    await store.Storage.ApplyAllConfiguredChangesToDatabaseAsync();
    
    // If in development, seed initial data
    if (app.Environment.IsDevelopment())
    {
        var initialData = serviceProvider.GetRequiredService<CatalogInitialData>();
        await initialData.InitializeAsync(serviceProvider);
        logger.LogInformation("Database seeded with initial data");
    }
    
    logger.LogInformation("Database migration completed");
});

app.UseHealthChecks("/health",
    new HealthCheckOptions
    {
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
    });

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.Run();

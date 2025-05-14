using BuildingBlocks.Messaging.MassTransit;
using Discount.gRPC;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using BuildingBlocks.Extensions;
using Prometheus;
using Basket.API.Metrics;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var assembly = typeof(Program).Assembly;
//Application Services
builder.Services.AddMediatR(config =>
{
    config.RegisterServicesFromAssembly(assembly);
    config.AddOpenBehavior(typeof(ValidationBehavior<,>));
    config.AddOpenBehavior(typeof(LoggingBehavior<,>));
});

builder.Services.AddValidatorsFromAssembly(assembly);

builder.Services.AddAutoMapper(assembly);
var DatabaseConnection = builder.Configuration.GetConnectionString("Database")!;
//Data Services
builder.Services.AddMarten(opts =>
{
    opts.Connection(DatabaseConnection);
    opts.Schema.For<ShoppingCart>().Identity(e => e.UserName);
}).UseLightweightSessions();

builder.Services.AddScoped<IBasketRepository, BasketRepository>();
builder.Services.Decorate<IBasketRepository, CachedBasketRepository>();

//MANUALLY DECOCARION
//builder.Services.AddScoped<IBasketRepository>(provider =>
//{
//    var basketRepository = provider.GetRequiredService<BasketRepository>();
//    return new CachedBasketRepository(basketRepository, provider.GetRequiredService<IDistributedCache>());
//});

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
});

//if (builder.Environment.IsDevelopment())
//{
//    builder.Services.InitializeMartenWith<CatalogInitialData>();
//}

// Add Prometheus monitoring
builder.Services.AddPrometheusMonitoring("basket-service");

// Add metrics background service
builder.Services.AddHostedService<BasketMetricsHostedService>();

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

//Cross-Cutting Services
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("Database")!)
    .AddRedis(builder.Configuration.GetConnectionString("Redis")!)
    .ForwardToPrometheus();

//gRPC Services
builder.Services.AddGrpcClient<DiscountProtoService.DiscountProtoServiceClient>(options =>
{
    options.Address = new Uri(builder.Configuration["gRPCSettings:DiscountUrl"]!);
})
    .ConfigurePrimaryHttpMessageHandler(() =>
    {
        var handler = new HttpClientHandler()
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
        return handler;
    });
//Async Communication Services
builder.Services.AddMessageBroker(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.

// First, use CORS to allow Prometheus to scrape metrics
app.UseCors("MetricsPolicy");

// Use routing
app.UseRouting();

// Register Prometheus HTTP metrics middleware
app.UseHttpMetrics();

app.UseMiddleware<CustomExceptionHandler>();

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

// Configure endpoints with metrics
app.UseEndpoints(endpoints =>
{
    // Ensure metrics endpoint is properly registered
    endpoints.MapMetrics("/metrics").AllowAnonymous();
    
    // Add metrics probe endpoint
    endpoints.MapGet("/metrics-probe", async context =>
    {
        context.Response.StatusCode = 200;
        await context.Response.WriteAsync("Metrics endpoint is working!");
    });
    
    // Map controllers
    endpoints.MapControllers();
});

// Initialize metrics
BasketMetrics.InitializeMetrics(app.Services);

app.Run();

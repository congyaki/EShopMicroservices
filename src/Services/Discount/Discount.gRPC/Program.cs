using BuildingBlocks.Extensions;
using BuildingBlocks.Services;
using Consul;
using Discount.gRPC.Data;
using Discount.gRPC.Services;
using Microsoft.EntityFrameworkCore;
using Prometheus;
using Discount.gRPC.Metrics;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// Additional configuration is required to successfully run gRPC on macOS.
// For instructions on how to configure Kestrel and gRPC clients on macOS, visit https://go.microsoft.com/fwlink/?linkid=2099682

// Add services to the container.
builder.Services.AddGrpc();
builder.Services.AddGrpcReflection();

builder.Services.AddDbContext<DiscountContext>(opts =>
{
    opts.UseSqlite(builder.Configuration.GetConnectionString("Database"));
});

var assembly = typeof(Program).Assembly;

builder.Services.AddAutoMapper(assembly);

// Add Prometheus monitoring
builder.Services.AddPrometheusMonitoring("discount-service");

// Add metrics background service
builder.Services.AddHostedService<DiscountMetricsHostedService>();

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

// Enhanced health checks with Prometheus metrics
builder.Services.AddHealthChecks()
    .AddCheck("database", () => {
        using var scope = builder.Services.BuildServiceProvider().CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DiscountContext>();
        try {
            db.Database.CanConnect();
            return HealthCheckResult.Healthy();
        } catch (Exception ex) {
            return HealthCheckResult.Unhealthy("Database connection failed", ex);
        }
    })
    .ForwardToPrometheus();

// Lấy cấu hình chung cho tất cả môi trường - cần thiết cho cả Development và Production
var serviceConfig = builder.Configuration.GetServiceConfig();
Console.WriteLine($"Configuring gRPC service: {serviceConfig.ServiceName} at {serviceConfig.ServiceAddress}:{serviceConfig.ServicePort}");

// Chỉ cấu hình seeding, Consul và Leader Election trong môi trường Development
if (builder.Environment.IsDevelopment())
{
    // Cấu hình DiscountSeedingOptions
    builder.Services.Configure<DiscountSeedingOptions>(options =>
    {
        builder.Configuration.GetSection("DiscountSeedingOptions").Bind(options);
        // Cấu hình mặc định nếu không có trong appsettings
        if (options.BatchSize <= 0) options.BatchSize = 200;
        if (options.DelayBetweenBatchesMs <= 0) options.DelayBetweenBatchesMs = 100;
        if (options.StartupDelaySeconds <= 0) options.StartupDelaySeconds = 5;
    });
    
    // Đăng ký Background Service để seed data - chỉ trong môi trường Development
    builder.Services.AddHostedService<DiscountDataSeedingService>();
    
    // Tạo service ID duy nhất từ serviceConfig - chỉ cần trong môi trường Development
    var serviceId = $"{serviceConfig.ServiceName}-{Guid.NewGuid()}";
    Console.WriteLine($"Generated unique service ID for Leader Election: {serviceId}");
    
    // Register Consul client - chỉ trong môi trường Development, sử dụng thông tin từ serviceConfig
    builder.Services.AddSingleton<IConsulClient>(p => new ConsulClient(consulConfig =>
    {
        consulConfig.Address = new Uri($"http://{serviceConfig.ConsulHost}:{serviceConfig.ConsulPort}");
    }));

    // Sử dụng Leader Election Factory để tự động chọn provider phù hợp - chỉ trong môi trường Development
    builder.Services.AddLeaderElection(builder.Configuration, builder.Environment);

    // Register ServiceDiscoveryHostedService - chỉ trong môi trường Development
    builder.Services.AddHostedService<ServiceDiscoveryHostedService>();
    
    Console.WriteLine("Consul client, Leader Election và Service Discovery đã được đăng ký trong môi trường Development");
}
else
{
    // Trong môi trường Production, sử dụng NoOpLeaderElectionService để tránh phụ thuộc vào Consul
    builder.Services.AddSingleton<ILeaderElectionService>(sp =>
    {
        var logger = sp.GetRequiredService<ILogger<NoOpLeaderElectionService>>();
        return new NoOpLeaderElectionService(logger);
    });
    
    Console.WriteLine($"Production mode: Using {serviceConfig.ServiceName} with NoOpLeaderElectionService");
}

var app = builder.Build();

// Configure the HTTP request pipeline.
// Đầu tiên, sử dụng CORS để cho phép Prometheus scrape metrics
app.UseCors("MetricsPolicy");

// Sử dụng routing
app.UseRouting();

// Đăng ký Prometheus HTTP metrics middleware
app.UseHttpMetrics();

if (app.Environment.IsDevelopment())
{
    app.MapGrpcReflectionService();
}

// Configure health checks and metrics endpoints
app.UseEndpoints(endpoints =>
{
    // Đăng ký gRPC endpoints
    endpoints.MapGrpcService<DiscountService>();
    
    // Đảm bảo metrics endpoint luôn được đăng ký đúng cách
    endpoints.MapMetrics("/metrics").AllowAnonymous();
    
    // Thêm endpoint kiểm tra health của metrics
    endpoints.MapGet("/metrics-probe", async context =>
    {
        context.Response.StatusCode = 200;
        await context.Response.WriteAsync("Metrics endpoint is working!");
    });
    
    // Đăng ký health check endpoint
    endpoints.MapHealthChecks("/health");
    
    // Map home page
    endpoints.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
});

// Configure database migration
app.UseMigration();

// Initialize metrics
DiscountMetrics.InitializeMetrics(app.Services);

app.Run();

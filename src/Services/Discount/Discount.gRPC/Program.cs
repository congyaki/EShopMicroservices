using BuildingBlocks.Extensions;
using BuildingBlocks.Services;
using Consul;
using Discount.gRPC.Data;
using Discount.gRPC.Services;
using Microsoft.EntityFrameworkCore;

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

// Cấu hình DiscountSeedingOptions
builder.Services.Configure<DiscountSeedingOptions>(options =>
{
    builder.Configuration.GetSection("DiscountSeedingOptions").Bind(options);
    // Cấu hình mặc định nếu không có trong appsettings
    if (options.BatchSize <= 0) options.BatchSize = 200;
    if (options.DelayBetweenBatchesMs <= 0) options.DelayBetweenBatchesMs = 100;
    if (options.StartupDelaySeconds <= 0) options.StartupDelaySeconds = 5;
});

// Lấy service ID duy nhất cho instance này - sử dụng trong Leader Election
var serviceConfig = builder.Configuration.GetServiceConfig();
var serviceId = $"{serviceConfig.ServiceName}-{Guid.NewGuid()}";

// Register Consul client
builder.Services.AddSingleton<IConsulClient>(p => new ConsulClient(consulConfig =>
{
    consulConfig.Address = new Uri($"http://{serviceConfig.ConsulHost}:{serviceConfig.ConsulPort}");
}));

// Đăng ký Leader Election Service
builder.Services.AddSingleton<ILeaderElectionService>(sp => 
{
    var consulClient = sp.GetRequiredService<IConsulClient>();
    var logger = sp.GetRequiredService<ILogger<ConsulLeaderElectionService>>();
    return new ConsulLeaderElectionService(
        consulClient, 
        "discount-service", 
        serviceId, 
        logger);
});

// Đăng ký Background Service để seed data
builder.Services.AddHostedService<DiscountDataSeedingService>();

// Register ServiceDiscoveryHostedService
builder.Services.AddHostedService<ServiceDiscoveryHostedService>();

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.MapGrpcReflectionService();
}

// Configure the HTTP request pipeline.
app.UseMigration();
app.MapGrpcService<DiscountService>();
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.Run();

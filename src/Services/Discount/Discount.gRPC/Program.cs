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
if (app.Environment.IsDevelopment())
{
    app.MapGrpcReflectionService();
}

// Configure the HTTP request pipeline.
app.UseMigration();
app.MapGrpcService<DiscountService>();
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.Run();


using AspNetCoreRateLimit;

var builder = WebApplication.CreateBuilder(args);

// Cấu hình YARP
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// Thêm cấu hình Rate Limiting
//builder.Services.AddOptions();
//builder.Services.AddMemoryCache();
//builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
//builder.Services.Configure<IpRateLimitPolicies>(builder.Configuration.GetSection("IpRateLimitPolicies"));
//builder.Services.AddInMemoryRateLimiting();
//builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();


var app = builder.Build();

// Sử dụng Middleware IP Rate Limiting
//app.UseIpRateLimiting();

// Thêm Reverse Proxy
app.MapReverseProxy();

// Lỗi là do thằng UseRouting này
//app.UseRouting();

app.Run();

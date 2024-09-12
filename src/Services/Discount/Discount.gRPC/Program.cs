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
var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.MapGrpcReflectionService();

}

// Configure the HTTP request pipeline.
app.UseMigration();
app.MapGrpcService<DiscountService>();

app.Run();

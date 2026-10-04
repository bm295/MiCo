using MilkCoPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using MilkCoPOS.Application.Ports;
using MilkCoPOS.Application.Services;
using MilkCoPOS.Data;
using MilkCoPOS.Infrastructure.Persistence;

var builder = Microsoft.AspNetCore.Builder.WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("DefaultConnection is not configured.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddScoped<IOrderRepositoryPort, OrderRepositoryAdapter>();
builder.Services.AddScoped<IInventoryRepositoryPort, InventoryRepositoryAdapter>();
builder.Services.AddScoped<IPaymentRepositoryPort, PaymentRepositoryAdapter>();
builder.Services.AddScoped<ITableRepositoryPort, TableRepositoryAdapter>();
builder.Services.AddScoped<IReportingRepositoryPort, ReportingRepositoryAdapter>();

builder.Services.AddScoped<IOrderUseCaseService, OrderUseCaseService>();
builder.Services.AddScoped<IInventoryUseCaseService, InventoryUseCaseService>();
builder.Services.AddScoped<IPaymentUseCaseService, PaymentUseCaseService>();
builder.Services.AddScoped<ITableUseCaseService, TableUseCaseService>();
builder.Services.AddScoped<IReportingUseCaseService, ReportingUseCaseService>();

builder.Services.AddControllers();

var app = builder.Build();

app.UseStaticFiles();

app.MapGet("/", context =>
{
    context.Response.Redirect("/orders");
    return Task.CompletedTask;
});

app.MapControllers();
// Unknown API routes must remain 404 rather than returning the SPA document.
app.MapFallback("/api/{**path}", () => Results.NotFound());
app.MapFallbackToFile("index.html");

app.Run();

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MilkCoPOS.Application.Models;
using MilkCoPOS.Application.Services;
using MilkCoPOS.Data;
using MilkCoPOS.Domain.Entities;
using MilkCoPOS.Infrastructure.Persistence;
using Xunit;

namespace WebApplication.Tests;

public class InventoryRestockingPersistenceTests
{
    [Fact]
    public async Task Settings_round_trip_and_recommendations_do_not_write_stock_or_payments()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var context = new ApplicationDbContext(options);
        await context.Database.EnsureCreatedAsync();
        var item = new InventoryItem { Name = "Milk", Quantity = 0, UnitPrice = 50000m };
        context.Inventory.Add(item);
        await context.SaveChangesAsync();
        var service = new InventoryUseCaseService(new InventoryRepositoryAdapter(context));

        await service.UpdateRestockingSettingsAsync(item.ItemId, new RestockingSettingsRequest
        {
            TargetStock = 3,
            PurchasePriceHistory = [999m, 0.20m, 0.20m, 0.20m, 0.20m, 0.10m]
        });
        context.ChangeTracker.Clear();

        var saved = await context.Inventory.SingleAsync();
        Assert.Equal(3, saved.TargetStock);
        Assert.Equal(new[] { 999m, 0.20m, 0.20m, 0.20m, 0.20m, 0.10m }, saved.PurchasePriceHistory);
        var result = await service.RecommendRestockingAsync(0.30m);
        Assert.Equal(3, Assert.Single(result.Purchases).Quantity);
        Assert.Equal(0m, result.RemainingBudget);
        Assert.False(context.ChangeTracker.HasChanges());
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        Assert.Equal(0, (await context.Inventory.SingleAsync()).Quantity);
        Assert.Empty(await context.Payments.ToListAsync());
    }
}

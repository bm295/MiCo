using Microsoft.AspNetCore.Mvc;
using MilkCoPOS.Application.Models;
using MilkCoPOS.Application.Ports;
using MilkCoPOS.Application.Services;
using MilkCoPOS.Controllers;
using MilkCoPOS.Domain.Entities;
using Xunit;

namespace WebApplication.Tests;

public class InventoryRestockingTests
{
    private static InventoryItem Item(int id, string name, int stock, int target, params decimal[] prices) =>
        new() { ItemId = id, Name = name, Quantity = stock, TargetStock = target,
            UnitPrice = 999999m, PurchasePriceHistory = [.. prices] };

    private static InventoryItem Milk(int stock = 2, int target = 7) =>
        Item(1, "Milk", stock, target, 30000m, 30000m, 30000m, 30000m, 20000m);

    private static InventoryItem Tea(int stock = 1, int target = 6) =>
        Item(2, "Tea", stock, target, 12000m, 12000m, 12000m, 12000m, 10000m);

    [Fact]
    public async Task Largest_discount_consumes_budget_first_without_changing_stock_or_saving()
    {
        var milk = Milk();
        var tea = Tea();
        var repository = new InventoryRepository(tea, milk);
        var service = new InventoryUseCaseService(repository);

        var result = await service.RecommendRestockingAsync(100000m);

        var purchase = Assert.Single(result.Purchases);
        Assert.Equal(1, purchase.ItemId);
        Assert.Equal(5, purchase.Quantity);
        Assert.Equal(20000m, purchase.PurchasePrice);
        Assert.Equal(100000m, purchase.TotalCost);
        Assert.Equal(0.40m, purchase.RelativeDiscount);
        Assert.Equal(0m, result.RemainingBudget);
        Assert.Equal(2, milk.Quantity);
        Assert.Equal(1, tea.Quantity);
        Assert.Equal(0, repository.SaveCalls);

        var teaOnly = await new InventoryUseCaseService(new InventoryRepository(tea))
            .RecommendRestockingAsync(10000m);
        Assert.Equal(0.16m, Assert.Single(teaOnly.Purchases).RelativeDiscount);
    }

    [Fact]
    public async Task Caps_at_shortage_and_allocates_remaining_budget_in_whole_units()
    {
        var result = await new InventoryUseCaseService(new InventoryRepository(Milk(8, 10), Tea(1, 10)))
            .RecommendRestockingAsync(95000m);

        Assert.Collection(result.Purchases,
            p => { Assert.Equal("Milk", p.ItemName); Assert.Equal(2, p.Quantity); Assert.Equal(40000m, p.TotalCost); },
            p => { Assert.Equal("Tea", p.ItemName); Assert.Equal(5, p.Quantity); Assert.Equal(50000m, p.TotalCost); });
        Assert.Equal(5000m, result.RemainingBudget);
    }

    [Fact]
    public async Task Skips_unaffordable_candidate_and_considers_next()
    {
        var result = await new InventoryUseCaseService(new InventoryRepository(Milk(0, 5), Tea(0, 5)))
            .RecommendRestockingAsync(15000m);

        var purchase = Assert.Single(result.Purchases);
        Assert.Equal("Tea", purchase.ItemName);
        Assert.Equal(1, purchase.Quantity);
        Assert.Equal(5000m, result.RemainingBudget);
    }

    [Fact]
    public async Task Excludes_non_discounted_and_sufficiently_stocked_items()
    {
        var service = new InventoryUseCaseService(new InventoryRepository(
            Item(1, "Milk", 2, 10, 10000m, 10000m, 10000m, 10000m, 20000m),
            Item(2, "Tea", 2, 10, 10000m, 10000m, 10000m, 10000m, 10000m),
            Item(3, "Sugar", 10, 10, 15000m, 15000m, 15000m, 15000m, 10000m),
            Item(4, "Coffee", 12, 10, 15000m, 15000m, 15000m, 15000m, 10000m)));

        var result = await service.RecommendRestockingAsync(100000m);

        Assert.Empty(result.Purchases);
        Assert.Equal(100000m, result.RemainingBudget);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1000)]
    public async Task Excludes_non_positive_current_price(int price)
    {
        var service = new InventoryUseCaseService(new InventoryRepository(
            Item(1, "Milk", 0, 10, 10000m, 10000m, 10000m, 10000m, price)));

        var result = await service.RecommendRestockingAsync(100000m);

        Assert.Empty(result.Purchases);
        Assert.Equal(100000m, result.RemainingBudget);
    }

    [Fact]
    public async Task Reports_missing_history_and_still_recommends_other_items()
    {
        var service = new InventoryUseCaseService(new InventoryRepository(
            Item(1, "Milk", 0, 10, 10000m, 10000m, 10000m, 10000m), Tea()));

        var result = await service.RecommendRestockingAsync(10000m);

        var warning = Assert.Single(result.Warnings);
        Assert.Equal(1, warning.ItemId);
        Assert.Equal("Milk has insufficient purchase-price history.", warning.Message);
        Assert.Equal("Tea", Assert.Single(result.Purchases).ItemName);
    }

    [Fact]
    public async Task Equal_discounts_are_ordered_by_item_id()
    {
        var service = new InventoryUseCaseService(new InventoryRepository(
            Item(2, "Tea", 0, 1, 15000m, 15000m, 15000m, 15000m, 10000m),
            Item(1, "Milk", 0, 1, 15000m, 15000m, 15000m, 15000m, 10000m)));

        var result = await service.RecommendRestockingAsync(10000m);

        Assert.Equal("Milk", Assert.Single(result.Purchases).ItemName);
        Assert.Equal(0m, result.RemainingBudget);
    }

    [Fact]
    public async Task Decimal_prices_preserve_exact_affordable_quantity()
    {
        var service = new InventoryUseCaseService(new InventoryRepository(
            Item(1, "Milk", 0, 3, 0.20m, 0.20m, 0.20m, 0.20m, 0.10m)));

        var result = await service.RecommendRestockingAsync(0.30m);

        var purchase = Assert.Single(result.Purchases);
        Assert.Equal(3, purchase.Quantity);
        Assert.Equal(0.30m, purchase.TotalCost);
        Assert.Equal(0.00m, result.RemainingBudget);
    }

    [Fact]
    public async Task Zero_budget_returns_no_purchases()
    {
        var result = await new InventoryUseCaseService(new InventoryRepository(Milk(), Tea()))
            .RecommendRestockingAsync(0m);

        Assert.Empty(result.Purchases);
        Assert.Equal(0m, result.RemainingBudget);
    }

    [Fact]
    public async Task Negative_budget_is_rejected_by_service_and_api()
    {
        var repository = new InventoryRepository(Milk());
        var service = new InventoryUseCaseService(repository);
        var error = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => service.RecommendRestockingAsync(-1000m));
        Assert.StartsWith("Purchasing budget must not be negative.", error.Message);

        var response = await new InventoryController(service)
            .RecommendRestocking(new RestockingRequest { Budget = -1000m });
        Assert.IsType<BadRequestObjectResult>(response.Result);
        Assert.Equal(0, repository.ReadCalls);
    }

    [Fact]
    public async Task Only_latest_five_observations_determine_discount()
    {
        var service = new InventoryUseCaseService(new InventoryRepository(
            Item(1, "Milk", 0, 1, 999999m, 15000m, 15000m, 15000m, 15000m, 10000m)));

        var result = await service.RecommendRestockingAsync(10000m);

        Assert.Equal(0.40m, Assert.Single(result.Purchases).RelativeDiscount);
    }

    [Fact]
    public async Task Large_budget_does_not_overflow_quantity_conversion()
    {
        var service = new InventoryUseCaseService(new InventoryRepository(Milk(8, 10)));
        var result = await service.RecommendRestockingAsync(100000000000000m);
        Assert.Equal(2, Assert.Single(result.Purchases).Quantity);
    }

    [Fact]
    public async Task Settings_can_be_updated_for_existing_inventory_without_changing_selling_price()
    {
        var milk = Milk();
        var repository = new InventoryRepository(milk);
        var service = new InventoryUseCaseService(repository);
        var request = new RestockingSettingsRequest { TargetStock = 15,
            PurchasePriceHistory = [10000m, 9000m, 8000m, 7000m, 6000m] };

        var result = await service.UpdateRestockingSettingsAsync(1, request);

        Assert.Same(milk, result);
        Assert.Equal(15, milk.TargetStock);
        Assert.Equal(request.PurchasePriceHistory, milk.PurchasePriceHistory);
        Assert.NotSame(request.PurchasePriceHistory, milk.PurchasePriceHistory);
        Assert.Equal(999999m, milk.UnitPrice);
        Assert.Equal(2, milk.Quantity);
        Assert.Equal(1, repository.SaveCalls);
        var response = await new InventoryController(service)
            .UpdateRestockingSettings(999, request);
        Assert.IsType<NotFoundResult>(response.Result);
        Assert.Equal(1, repository.SaveCalls);
    }

    [Fact]
    public async Task Api_returns_recommendation()
    {
        var controller = new InventoryController(new InventoryUseCaseService(new InventoryRepository(Milk())));
        var response = await controller.RecommendRestocking(new RestockingRequest { Budget = 100000m });
        var body = Assert.IsType<RestockingRecommendation>(Assert.IsType<OkObjectResult>(response.Result).Value);
        Assert.Equal(5, Assert.Single(body.Purchases).Quantity);
    }

    private sealed class InventoryRepository(params InventoryItem[] items) : IInventoryRepositoryPort
    {
        public int SaveCalls { get; private set; }
        public int ReadCalls { get; private set; }
        public Task<List<InventoryItem>> GetAllAsync()
        {
            ReadCalls++;
            return Task.FromResult(items.ToList());
        }
        public Task<InventoryItem?> GetByIdAsync(int itemId) =>
            Task.FromResult(items.FirstOrDefault(i => i.ItemId == itemId));
        public Task<List<InventoryItem>> GetByIdsAsync(IEnumerable<int> itemIds) =>
            Task.FromResult(items.Where(i => itemIds.Contains(i.ItemId)).ToList());
        public Task<InventoryItem> AddAsync(InventoryItem item) =>
            throw new InvalidOperationException("Recommendations must not create inventory.");
        public Task SaveChangesAsync()
        {
            SaveCalls++;
            return Task.CompletedTask;
        }
    }
}

using MilkCoPOS.Application.Ports;
using MilkCoPOS.Domain.Entities;
using MilkCoPOS.Application.Models;

namespace MilkCoPOS.Application.Services;

public class InventoryUseCaseService(IInventoryRepositoryPort inventoryRepository) : IInventoryUseCaseService
{
    public Task<List<InventoryItem>> GetInventoryAsync() => inventoryRepository.GetAllAsync();

    public Task<InventoryItem?> GetInventoryItemAsync(int id) => inventoryRepository.GetByIdAsync(id);

    public Task<InventoryItem> CreateItemAsync(InventoryItem item) => inventoryRepository.AddAsync(item);

    public async Task<InventoryItem?> UpdateRestockingSettingsAsync(int id, RestockingSettingsRequest request)
    {
        if (request.TargetStock < 0)
            throw new ArgumentOutOfRangeException(nameof(request.TargetStock));

        var item = await inventoryRepository.GetByIdAsync(id);
        if (item is null)
            return null;

        item.TargetStock = request.TargetStock;
        item.PurchasePriceHistory = [.. request.PurchasePriceHistory];
        await inventoryRepository.SaveChangesAsync();
        return item;
    }

    public async Task<RestockingRecommendation> RecommendRestockingAsync(decimal budget)
    {
        if (budget < 0)
            throw new ArgumentOutOfRangeException(nameof(budget), "Purchasing budget must not be negative.");

        var purchases = new List<RecommendedPurchase>();
        var warnings = new List<RestockingWarning>();
        var candidates = new List<(InventoryItem Item, decimal Price, decimal Discount)>();

        foreach (var item in await inventoryRepository.GetAllAsync())
        {
            if (item.Quantity >= item.TargetStock)
                continue;

            if (item.PurchasePriceHistory.Count < 5)
            {
                warnings.Add(new(item.ItemId, item.Name,
                    $"{item.Name} has insufficient purchase-price history."));
                continue;
            }

            var prices = item.PurchasePriceHistory.TakeLast(5).ToArray();
            var currentPrice = prices[^1];
            if (currentPrice <= 0)
                continue;

            var discount = prices.Average() / currentPrice - 1m;
            if (discount > 0)
                candidates.Add((item, currentPrice, discount));
        }

        foreach (var candidate in candidates.OrderByDescending(c => c.Discount).ThenBy(c => c.Item.ItemId))
        {
            // Cap before converting to int, even when the budget can buy billions of units.
            var shortage = (long)candidate.Item.TargetStock - candidate.Item.Quantity;
            var quantity = (int)Math.Min(int.MaxValue,
                Math.Min(shortage, decimal.Floor(budget / candidate.Price)));
            if (quantity <= 0)
                continue;

            purchases.Add(new(candidate.Item.ItemId, candidate.Item.Name,
                quantity, candidate.Price, candidate.Discount));
            budget -= quantity * candidate.Price;
        }

        return new(purchases, budget, warnings);
    }
}

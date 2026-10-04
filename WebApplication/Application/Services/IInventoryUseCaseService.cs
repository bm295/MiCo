using MilkCoPOS.Domain.Entities;
using MilkCoPOS.Application.Models;

namespace MilkCoPOS.Application.Services;

public interface IInventoryUseCaseService
{
    Task<List<InventoryItem>> GetInventoryAsync();
    Task<InventoryItem?> GetInventoryItemAsync(int id);
    Task<InventoryItem> CreateItemAsync(InventoryItem item);
    Task<InventoryItem?> UpdateRestockingSettingsAsync(int id, RestockingSettingsRequest request);
    Task<RestockingRecommendation> RecommendRestockingAsync(decimal budget);
}

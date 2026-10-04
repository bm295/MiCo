using System.ComponentModel.DataAnnotations;

namespace MilkCoPOS.Application.Models;

public class RestockingRequest
{
    public decimal Budget { get; set; }
}

public class RestockingSettingsRequest
{
    [Range(0, int.MaxValue)]
    public int TargetStock { get; set; }

    [Required]
    public List<decimal> PurchasePriceHistory { get; set; } = [];
}

public record RecommendedPurchase(int ItemId, string ItemName, int Quantity,
    decimal PurchasePrice, decimal RelativeDiscount)
{
    public decimal TotalCost => Quantity * PurchasePrice;
}

public record RestockingWarning(int ItemId, string ItemName, string Message);

public record RestockingRecommendation(
    List<RecommendedPurchase> Purchases,
    decimal RemainingBudget,
    List<RestockingWarning> Warnings);

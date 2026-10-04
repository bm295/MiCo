using System.ComponentModel.DataAnnotations;

namespace MilkCoPOS.Domain.Entities;

public class InventoryItem
{
    public int ItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string Unit { get; set; } = "portion";
    [Range(0, int.MaxValue)]
    public int TargetStock { get; set; }
    // Oldest to newest; the final observation is the current purchase price.
    public List<decimal> PurchasePriceHistory { get; set; } = [];
}

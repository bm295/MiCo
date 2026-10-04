using Microsoft.AspNetCore.Mvc;
using MilkCoPOS.Application.Services;
using MilkCoPOS.Domain.Entities;
using MilkCoPOS.Application.Models;

namespace MilkCoPOS.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InventoryController(IInventoryUseCaseService inventoryService) : ControllerBase
{
    [HttpPost("restocking-recommendations")]
    public async Task<ActionResult<RestockingRecommendation>> RecommendRestocking(RestockingRequest request)
    {
        if (request.Budget < 0)
            return BadRequest(new { error = "Purchasing budget must not be negative." });

        return Ok(await inventoryService.RecommendRestockingAsync(request.Budget));
    }

    [HttpPut("{id:int}/restocking-settings")]
    public async Task<ActionResult<InventoryItem>> UpdateRestockingSettings(int id, RestockingSettingsRequest request)
    {
        var item = await inventoryService.UpdateRestockingSettingsAsync(id, request);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpGet]
    public async Task<ActionResult<List<InventoryItem>>> GetInventory() =>
        Ok(await inventoryService.GetInventoryAsync());

    [HttpPost]
    public async Task<ActionResult<InventoryItem>> CreateItem([FromBody] InventoryItem item)
    {
        var created = await inventoryService.CreateItemAsync(item);
        return CreatedAtAction(nameof(GetItem), new { id = created.ItemId }, created);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<InventoryItem>> GetItem(int id)
    {
        var item = await inventoryService.GetInventoryItemAsync(id);
        return item is null ? NotFound() : Ok(item);
    }
}

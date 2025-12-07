using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using OrderApi.Dtos;
using OrderApi.Services;
using Shared.Common;
using System.Text.Json;

namespace OrderApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class OrdersController(IOrdersService ordersService, ILogger<OrdersController> logger, IDistributedCache cache) : ControllerBase
{
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(OrderResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<OrderResponse>> GetOrder(Guid id, CancellationToken cts)
    {
        if (id == Guid.Empty)
        {
            logger.LogWarning("GetOrder called with an empty GUID.");
            return BadRequest("Invalid order ID.");
        }

        logger.LogInformation("Retrieving order with ID: {OrderId}", id);

        var cached = await cache.GetStringAsync($"{Constants.CacheKeyOrderPrefix}{id}", cts);
        if (!string.IsNullOrWhiteSpace(cached))
        {
            logger.LogInformation("Order with ID: {id} found in cache.", id);
            return Ok(JsonSerializer.Deserialize<OrderResponse>(cached));
        }

        var order = await ordersService.GetOrderByIdAsync(id, cts);

        return Ok(order);
    }

    [HttpGet("by-user/{userId}")]
    [ProducesResponseType(typeof(List<OrderResponse>), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<OrderResponse>> GetOrdersByUserId(Guid userId, CancellationToken cts)
    {
        if (userId == Guid.Empty)
        {
            logger.LogWarning("GetOrder called with an empty UserId GUID.");
            return BadRequest("Invalid user ID.");
        }

        logger.LogInformation("Retrieving orders by user: {UserId}", userId);
        var order = await ordersService.GetOrdersByUserAsync(userId, cts);

        return Ok(order);
    }

    [HttpPost]
    [ProducesResponseType(typeof(OrderResponse), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<ActionResult> CreateOrder(OrderCreationRequest? newOrder, CancellationToken cts)
    {
        if (IsInValidRequest(newOrder))
        {
            logger.LogWarning("CreateOrder called with invalid data.");
            return BadRequest("Invalid request data.");
        }

        logger.LogInformation("Creating a new order: {@NewOrder}", JsonSerializer.Serialize(newOrder));
        var createdOrder = await ordersService.CreateOrderAsync(newOrder!, cts);

        await cache.SetStringAsync(
           $"{Constants.CacheKeyOrderPrefix}{createdOrder.Id}",
           JsonSerializer.Serialize(createdOrder),
           new DistributedCacheEntryOptions
           {
               AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
           }, cts);

        return CreatedAtAction(nameof(GetOrder), new { id = createdOrder.Id }, createdOrder);
    }

    private static bool IsInValidRequest(OrderCreationRequest? newOrder)
    {
        return newOrder is null
            || string.IsNullOrWhiteSpace(newOrder?.Product)
            || newOrder.UserId == Guid.Empty
            || newOrder.Quantity <= 0
            || newOrder.Price < 0;
    }
}
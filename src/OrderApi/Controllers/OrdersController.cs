using Microsoft.AspNetCore.Mvc;
using OrderApi.Dtos;
using OrderApi.Services;

namespace OrderApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class OrdersController : ControllerBase
{

    private readonly IOrdersService _orderService;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(IOrdersService orderService, ILogger<OrdersController> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(OrderResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<OrderResponse>> GetOrder(Guid id)
    {
        if (id == Guid.Empty)
        {
            _logger.LogWarning("GetOrder called with an empty GUID.");
            return BadRequest("Invalid order ID.");
        }

        _logger.LogInformation("Retrieving order with ID: {OrderId}", id);
        var order = await _orderService.GetOrderByIdAsync(id);

        return Ok(order);
    }

    [HttpGet("by-user/{userId}")]
    [ProducesResponseType(typeof(List<OrderResponse>), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<OrderResponse>> GetOrdersByUserId(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            _logger.LogWarning("GetOrder called with an empty UserId GUID.");
            return BadRequest("Invalid user ID.");
        }

        _logger.LogInformation("Retrieving orders by user: {UserId}", userId);
        var order = await _orderService.GetOrdersByUserAsync(userId);

        return Ok(order);
    }

    [HttpPost]
    [ProducesResponseType(typeof(OrderResponse), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<ActionResult> CreateOrder(OrderCreationRequest? newOrder)
    {
        if (IsInValidRequest(newOrder))
        {
            _logger.LogWarning("CreateOrder called with invalid data.");
            return BadRequest("Invalid request data.");
        }

        _logger.LogInformation("Creating a new order with UserId: {UserId}, Product: {Product}", newOrder!.UserId, newOrder.Product);
        var createdOrder = await _orderService.CreateOrderAsync(newOrder);

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
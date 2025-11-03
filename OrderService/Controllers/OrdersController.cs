using Microsoft.AspNetCore.Mvc;
using OrderService.Dtos;
using OrderService.Services;
using Shared.Exceptions;

namespace OrderService.Controllers;

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
        try
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
        catch (NotFoundException)
        {
            _logger.LogWarning("Order with ID: {OrderId} not found.", id);
            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving Order with ID: {OrderId}", id);
            return StatusCode(500, "An error occurred while processing your request.");
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(OrderResponse), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<OrderResponse>> CreateOrder(OrderCreationRequest newOrder)
    {
        try
        {
            if (newOrder is null || string.IsNullOrWhiteSpace(newOrder?.Product) || newOrder.UserId == Guid.Empty || newOrder.Quantity == 0)
            {
                _logger.LogWarning("CreateOrder called with invalid data.");
                return BadRequest("Invalid request data.");
            }

            _logger.LogInformation("Creating a new order with UserId: {UserId}, Product: {Product}", newOrder.UserId, newOrder.Product);
            var createdOrder = await _orderService.CreateOrderAsync(newOrder);
            return CreatedAtAction(nameof(GetOrder), new { id = createdOrder.Id }, createdOrder);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while creating order");
            return StatusCode(500, "An error occurred while processing your request.");
        }
    }
}

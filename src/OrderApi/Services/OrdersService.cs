using OrderApi.Data;
using OrderApi.Dtos;
using Shared.Contracts;
using Shared.Exceptions;
using Shared.Models;

namespace OrderApi.Services;

public class OrdersService : IOrdersService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IKafkaProducerWrapper _producer;
    private readonly ILogger<OrdersService> _logger;

    public OrdersService(IOrderRepository orderRepository, IKafkaProducerWrapper kafkaProducer, ILogger<OrdersService> logger)
    {
        _orderRepository = orderRepository;
        _producer = kafkaProducer;
        _logger = logger;
    }

    public async Task<OrderResponse> CreateOrderAsync(OrderCreationRequest newOrder)
    {
        try
        {
            var order = newOrder.MapToOrder();
            bool isValidUser = await ValidateUser(order);
            if (!isValidUser)
            {
                _logger.LogError("Attempted to create order for unknown user ID {UserId}", order.UserId);
                throw new BadRequestException($"Known user with ID {order.UserId} not found.");
            }

            var createdOrder = await _orderRepository.CreateOrderAsync(order);

            if (createdOrder == null)
            {
                _logger.LogError("Failed to create order for user ID {UserId}", order.UserId);
                throw new Exception("Order creation failed.");
            }

            //await _producer.ProduceAsync(createdOrder.Id,
            //        new OrderCreatedEvent
            //        {
            //            Id = createdOrder.Id,
            //            UserId = createdOrder.UserId,
            //            Price = createdOrder.Price,
            //            Product = createdOrder.Product,
            //            Quantity = createdOrder.Quantity
            //        });

            return OrderResponse.MapOrderToResponseDto(createdOrder);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while creating order");
            throw;
        }
    }

    public async Task<OrderResponse> GetOrderByIdAsync(Guid id)
    {
        try
        {
            var response = await _orderRepository.GetOrderByIdAsync(id);
            return response == null ? throw new NotFoundException($"Order with ID {id} not found.")
                : OrderResponse.MapOrderToResponseDto(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving Order with ID: {OrderId}", id);
            throw;
        }
    }

    public async Task CreateKnownUserAsync(KnownUser knownUser)
    {
        try
        {
            var existingUser = await _orderRepository.GetKnownUserByIdAsync(knownUser.UserId);
            if (existingUser == null)
            {
                _logger.LogInformation("Creating new known user with ID {UserId}", knownUser.UserId);
                await _orderRepository.CreateKnownUserAsync(knownUser);
            }
        }
        catch (Exception)
        {
            _logger.LogError("Error occurred while creating known user with ID {UserId}", knownUser.UserId);
            throw;
        }
    }

    private async Task<bool> ValidateUser(Order order)
    {
        //var existingUser = await _orderRepository.GetKnownUserByIdAsync(order.UserId);
        //return existingUser != null;
 
        return true;
    }

    public async Task<List<OrderResponse>> GetOrdersByUserAsync(Guid userId)
    {
        var orders = await _orderRepository.GetOrdersByUserAsync(userId);
        return [.. orders.Select(o => OrderResponse.MapOrderToResponseDto(o))];
    }
}
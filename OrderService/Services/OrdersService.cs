using OrderService.Data;
using OrderService.Dtos;
using Shared.Contracts;
using Shared.Exceptions;
using Shared.Models;

namespace OrderService.Services;

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
        var order = newOrder.MapToOrder();
        bool isValidUser = await ValidateUser(order);
        if (!isValidUser)
        {
            _logger.LogError("Attempted to create order for unknown user ID {UserId}", order.UserId);
            throw new NotFoundException($"Known user with ID {order.UserId} not found.");
        }

        var createdOrder = await _orderRepository.CreateOrderAsync(order)
               .ContinueWith(task => OrderResponse.MapOrderToResponseDto(task.Result));

        await _producer.ProduceAsync(createdOrder.Id,
                new OrderCreatedEvent
                {
                    Id = createdOrder.Id,
                    UserId = createdOrder.UserId,
                    Price = createdOrder.Price,
                    Product = createdOrder.Product,
                    Quantity = createdOrder.Quantity
                });

        return createdOrder;
    }

    public async Task<OrderResponse> GetOrderByIdAsync(Guid id)
    {
        var response = await _orderRepository.GetOrderByIdAsync(id);
        return response == null ? throw new NotFoundException($"Order with ID {id} not found.") : OrderResponse.MapOrderToResponseDto(response);
    }

    public async Task<KnownUser> CreateKnownUserAsync(KnownUser knownUser)
    {
        var existingUser = await _orderRepository.GetKnownUserByIdAsync(knownUser.UserId);
        if (existingUser != null)
        {
            return existingUser;
        }
        _logger.LogInformation("Creating new known user with ID {UserId}", knownUser.UserId);
        return await _orderRepository.CreateKnownUserAsync(knownUser);
    }

    private async Task<bool> ValidateUser(Order order)
    {
        var existingUser = await _orderRepository.GetKnownUserByIdAsync(order.UserId);
        return existingUser != null;
    }
}

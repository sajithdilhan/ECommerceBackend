using OrderApi.Data;
using OrderApi.Dtos;
using Shared.Contracts;
using Shared.Exceptions;
using Shared.Models;

namespace OrderApi.Services;

public class OrdersService(IOrderRepository orderRepository, ILogger<OrdersService> logger, IKafkaProducerWrapper producer) : IOrdersService
{
    public async Task<OrderResponse> CreateOrderAsync(OrderCreationRequest newOrder)
    {
        try
        {
            var order = newOrder.MapToOrder();
            bool isValidUser = await ValidateUser(order);
            if (!isValidUser)
            {
                logger.LogError("Attempted to create order for unknown user ID {UserId}", order.UserId);
                throw new BadRequestException($"Known user with ID {order.UserId} not found.");
            }

            var createdOrder = await orderRepository.CreateOrderAsync(order);

            if (createdOrder == null)
            {
                logger.LogError("Failed to create order for user ID {UserId}", order.UserId);
                throw new Exception("Order creation failed.");
            }

            await producer.ProduceAsync(createdOrder.Id,
                    new OrderCreatedEvent
                    {
                        Id = createdOrder.Id,
                        UserId = createdOrder.UserId,
                        Price = createdOrder.Price,
                        Product = createdOrder.Product,
                        Quantity = createdOrder.Quantity
                    });

            return OrderResponse.MapOrderToResponseDto(createdOrder);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while creating order");
            throw;
        }
    }

    public async Task<OrderResponse> GetOrderByIdAsync(Guid id)
    {
        try
        {
            var response = await orderRepository.GetOrderByIdAsync(id);
            return response == null ? throw new NotFoundException($"Order with ID {id} not found.")
                : OrderResponse.MapOrderToResponseDto(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while retrieving Order with ID: {OrderId}", id);
            throw;
        }
    }

    public async Task CreateKnownUserAsync(KnownUser knownUser)
    {
        try
        {
            var existingUser = await orderRepository.GetKnownUserByIdAsync(knownUser.UserId);
            if (existingUser == null)
            {
                logger.LogInformation("Creating new known user with ID {UserId}", knownUser.UserId);
                await orderRepository.CreateKnownUserAsync(knownUser);
            }
        }
        catch (Exception)
        {
            logger.LogError("Error occurred while creating known user with ID {UserId}", knownUser.UserId);
            throw;
        }
    }

    private async Task<bool> ValidateUser(Order order)
    {
        var existingUser = await orderRepository.GetKnownUserByIdAsync(order.UserId);
        return existingUser != null;
    }

    public async Task<List<OrderResponse>> GetOrdersByUserAsync(Guid userId)
    {
        var orders = await orderRepository.GetOrdersByUserAsync(userId);
        return orders.Count == 0 ? throw new NotFoundException($"Order with UserId {userId} not found.") :
         [.. orders.Select(o => OrderResponse.MapOrderToResponseDto(o))];
    }
}
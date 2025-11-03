using OrderService.Data;
using OrderService.Dtos;
using Shared.Exceptions;

namespace OrderService.Services;

public class OrdersService : IOrdersService
{
    private readonly IOrderRepository _orderRepository;

    public OrdersService(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public Task<OrderResponse> CreateOrderAsync(OrderCreationRequest newOrder)
    {
        var order = newOrder.MapToOrder();
        return _orderRepository.CreateOrderAsync(order)
               .ContinueWith(task => OrderResponse.MapOrderToResponseDto(task.Result));
    }
    public async Task<OrderResponse> GetOrderByIdAsync(Guid id)
    {
        var response = await _orderRepository.GetOrderByIdAsync(id);
        return response == null ? throw new NotFoundException($"Order with ID {id} not found.") : OrderResponse.MapOrderToResponseDto(response);
    }
}

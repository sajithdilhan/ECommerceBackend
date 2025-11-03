using OrderService.Dtos;

namespace OrderService.Services;

public interface IOrdersService
{
    public Task<OrderResponse> CreateOrderAsync(OrderCreationRequest newOrder);
    public Task<OrderResponse> GetOrderByIdAsync(Guid id);
}

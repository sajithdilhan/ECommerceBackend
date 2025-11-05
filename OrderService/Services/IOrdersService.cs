using OrderService.Dtos;
using Shared.Models;

namespace OrderService.Services;

public interface IOrdersService
{
    public Task<OrderResponse> CreateOrderAsync(OrderCreationRequest newOrder);
    public Task<OrderResponse> GetOrderByIdAsync(Guid id);
    public Task<KnownUser> CreateKnownUserAsync(KnownUser knownUser);
}

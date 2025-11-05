using OrderApi.Dtos;
using Shared.Models;

namespace OrderApi.Services;

public interface IOrdersService
{
    public Task<OrderResponse> CreateOrderAsync(OrderCreationRequest newOrder);
    public Task<OrderResponse> GetOrderByIdAsync(Guid id);
    public Task CreateKnownUserAsync(KnownUser knownUser);
}

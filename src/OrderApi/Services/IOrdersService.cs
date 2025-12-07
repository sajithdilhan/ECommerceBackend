using OrderApi.Dtos;
using Shared.Models;

namespace OrderApi.Services;

public interface IOrdersService
{
    public Task<OrderResponse> CreateOrderAsync(OrderCreationRequest newOrder, CancellationToken cts);
    public Task<OrderResponse> GetOrderByIdAsync(Guid id, CancellationToken cts);
    public Task<List<OrderResponse>> GetOrdersByUserAsync(Guid userId, CancellationToken cts);
    public Task CreateKnownUserAsync(KnownUser knownUser, CancellationToken cts);
}

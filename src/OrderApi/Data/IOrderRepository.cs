using Shared.Models;

namespace OrderApi.Data
{
    public interface IOrderRepository 
    {
        Task<Order?> GetOrderByIdAsync(Guid id);
        Task<List<Order>> GetOrdersByUserAsync(Guid userId);
        Task<Order?> CreateOrderAsync(Order newOrder);
        Task<int> CreateKnownUserAsync(KnownUser knownUser);
        Task<KnownUser?> GetKnownUserByIdAsync(Guid id);
    }
}

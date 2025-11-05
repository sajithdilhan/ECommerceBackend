using Shared.Models;

namespace OrderService.Data
{
    public interface IOrderRepository 
    {
        Task<Order?> GetOrderByIdAsync(Guid id);
        Task<Order> CreateOrderAsync(Order newOrder);
        Task<KnownUser> CreateKnownUserAsync(KnownUser knownUser);
        Task<KnownUser?> GetKnownUserByIdAsync(Guid id);
    }
}

using Shared.Models;

namespace OrderService.Data
{
    public class OrderRepository : IOrderRepository
    {
        private readonly OrderDbContext _context;

        public OrderRepository(OrderDbContext context)
        {
            _context = context;
        }

        public async Task<Order> CreateOrderAsync(Order newOrder)
        {
            _context.Orders.Add(newOrder);
            _context.SaveChanges();
            return newOrder;
        }

        public async Task<Order?> GetOrderByIdAsync(Guid id)
        {
            return await _context.Orders.FindAsync(id);
        }
    }
}

using Microsoft.EntityFrameworkCore;
using Shared.Models;

namespace OrderApi.Data;

public class OrderRepository : IOrderRepository
{
    private readonly OrderDbContext _context;

    public OrderRepository(OrderDbContext context)
    {
        _context = context;
    }

    public async Task<int> CreateKnownUserAsync(KnownUser knownUser)
    {
        await _context.KnownUsers.AddAsync(knownUser);
        return await _context.SaveChangesAsync();
    }

    public async Task<Order?> CreateOrderAsync(Order newOrder)
    {
        await _context.Orders.AddAsync(newOrder);
        await _context.SaveChangesAsync();
        return newOrder;
    }

    public async Task<KnownUser?> GetKnownUserByIdAsync(Guid id)
    {
        return await _context.KnownUsers.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == id);
    }

    public async Task<Order?> GetOrderByIdAsync(Guid id)
    {
        return await _context.Orders.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
    }
}

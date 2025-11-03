using Microsoft.EntityFrameworkCore;
using Shared.Models;

namespace OrderService.Data
{
    public class OrderDbContext(DbContextOptions<OrderDbContext> options) : DbContext(options)
    {
        public DbSet<Order> Orders { get; set; }
    }
}

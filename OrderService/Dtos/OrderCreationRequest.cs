using Shared.Models;
using System.ComponentModel.DataAnnotations;

namespace OrderService.Dtos
{
    public class OrderCreationRequest
    {
        [Required]
        public Guid UserId { get; set; }

        [Required]
        public string Product { get; set; } = string.Empty;

        [Required]
        public int Quantity { get; set; }

        [Required]
        public decimal Price { get; set; }

        public Order MapToOrder()
        {
            return new Order
            {
                Id = Guid.NewGuid(),
                UserId = this.UserId,
                Product = this.Product,
                Quantity = this.Quantity,
                Price = this.Price
            };
        }
    }
}

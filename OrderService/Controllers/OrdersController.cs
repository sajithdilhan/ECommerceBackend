using Microsoft.AspNetCore.Mvc;
using OrderService.Models;

namespace OrderService.Controllers;

[Route("api/[controller]")]
[ApiController]
public class OrdersController : ControllerBase
{
    [HttpGet("{id}")]
    public ActionResult<Order> GetOrder(Guid id)
    {
        // For demonstration purposes, returning a dummy order
        Guid userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var user = new Order { Id = id, UserId = userId, Product = "Product 1", Quantity = 1, Price = 38m };
        return Ok(user);
    }
}

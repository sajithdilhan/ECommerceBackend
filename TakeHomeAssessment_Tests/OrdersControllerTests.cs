using Microsoft.AspNetCore.Mvc;
using OrderService.Controllers;
using OrderService.Models;
using UserService.Models;

namespace TakeHomeAssessment_Tests;

public class OrdersControllerTests
{
    [Fact]
    public void GetOrder_ReturnsOrder_WhenOrderExists()
    {
        // Arrange
        Guid orderId = Guid.NewGuid();
        Guid userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var expectedUser = new Order { Id = orderId, UserId = userId, Product = "Product 1", Quantity = 1, Price = 38m };

        var controller = new OrdersController();

        // Act
        var result = controller.GetOrder(orderId);

        // Assert
        Assert.NotNull(result);
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var orderResult = Assert.IsType<Order>(okResult.Value);
        Assert.NotNull(orderResult);
        Assert.Equal(orderId, orderResult.Id);
        Assert.Equal(userId, orderResult.UserId);
        Assert.Equal("Product 1", orderResult.Product);
        Assert.Equal(1, orderResult.Quantity);
        Assert.Equal(38m, orderResult.Price);
    }
}

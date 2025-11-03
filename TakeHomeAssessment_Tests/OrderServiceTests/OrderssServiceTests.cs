using Moq;
using OrderService.Data;
using OrderService.Dtos;
using OrderService.Services;
using Shared.Exceptions;
using Shared.Models;

namespace TakeHomeAssessment_Tests.UserServiceTests;

public class OrdersServiceTests
{
    private readonly Mock<IOrderRepository> _orderRepository;

    public OrdersServiceTests()
    {
        _orderRepository = new Mock<IOrderRepository>();
    }

    [Fact]
    public async Task GetOrderByIdAsync_ReurnsOrder_WhenOrderExists()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        _orderRepository.Setup(repo => repo.GetOrderByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new Order
            {
                UserId = new Guid(),
                Id = orderId,
                Price = 100,
                Product = "Test Product",
                Quantity = 2
            });

        var ordersService = new OrdersService(_orderRepository.Object);

        // Act
        var result = await ordersService.GetOrderByIdAsync(orderId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(orderId, result.Id);
    }

    [Fact]
    public async Task GetOrderByIdAsync_ThrowsNotFoundException_WhenOrderDoesNotExist()
    {
        // Arrange
        Guid orderId = Guid.NewGuid();
        _orderRepository.Setup(repo => repo.GetOrderByIdAsync(orderId)).ReturnsAsync((Order?)null);
        var ordersService = new OrdersService(_orderRepository.Object);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<NotFoundException>(
            () => ordersService.GetOrderByIdAsync(orderId)
        );
    }

    [Fact]
    public async Task GetOrderByIdAsync_ThrowsException_WhenDb_Exception()
    {
        // Arrange
        Guid orderId = Guid.NewGuid();
        _orderRepository.Setup(repo => repo.GetOrderByIdAsync(orderId)).ThrowsAsync(new Exception("Database error"));

        var usersService = new OrdersService(_orderRepository.Object);


        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(
            () => usersService.GetOrderByIdAsync(orderId)
        );

        Assert.Equal("Database error", ex.Message);
        _orderRepository.Verify(r => r.GetOrderByIdAsync(It.IsAny<Guid>()), Times.Once);

    }

    [Fact]
    public async Task CreateOrder_Returns_CreatedOrder()
    {
        // Arrange
        var newOrderRequest = new OrderCreationRequest
        {
            UserId = Guid.NewGuid(),
            Product = "Test Product",
            Quantity = 2,
            Price = 100
        };

        var createdOrder = new Order
        {
            Id = Guid.NewGuid(),
            UserId = newOrderRequest.UserId,
            Product = newOrderRequest.Product,
            Quantity = newOrderRequest.Quantity,
            Price = newOrderRequest.Price
        };

        _orderRepository.Setup(repo => repo.CreateOrderAsync(It.IsAny<Order>()))
            .ReturnsAsync(createdOrder);

    }
}

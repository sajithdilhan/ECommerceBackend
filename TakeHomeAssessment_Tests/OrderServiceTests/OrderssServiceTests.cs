using Microsoft.Extensions.Logging;
using Moq;
using OrderService.Data;
using OrderService.Dtos;
using OrderService.Services;
using Shared.Contracts;
using Shared.Exceptions;
using Shared.Models;

namespace TakeHomeAssessment_Tests.OrderServiceTests;

public class OrdersServiceTests
{
    private readonly Mock<IOrderRepository> _orderRepository;
    private readonly Mock<IKafkaProducerWrapper> _kfkaProducer;
    private readonly Mock<ILogger<OrdersService>> _logger;


    public OrdersServiceTests()
    {
        _orderRepository = new Mock<IOrderRepository>();
        _kfkaProducer = new Mock<IKafkaProducerWrapper>();
        _logger = new Mock<ILogger<OrdersService>>();
    }

    [Fact]
    public async Task CreateOrder_ReturnsNotFound_WhenUserNotFound()
    {
        // Arrange
        var newOrderRequest = new OrderCreationRequest
        {
            UserId = Guid.NewGuid(),
            Product = "Test Product",
            Quantity = 2,
            Price = 100
        };

        _orderRepository.Setup(repo => repo.GetKnownUserByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(null as KnownUser);
        var orderService = new OrdersService(_orderRepository.Object, _kfkaProducer.Object, _logger.Object);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<NotFoundException>(() => orderService.CreateOrderAsync(newOrderRequest));
        Assert.Contains($"Known user with ID {newOrderRequest.UserId} not found.", ex.Message);
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
        var knownUser = new KnownUser
        {
            UserId = newOrderRequest.UserId,
            Email = "sajith@mail.com"
        };
        var orderId = Guid.NewGuid();
        var createdOrder = new Order
        {
            Id = orderId,
            UserId = newOrderRequest.UserId,
            Product = newOrderRequest.Product,
            Quantity = newOrderRequest.Quantity,
            Price = newOrderRequest.Price
        };

        _orderRepository.Setup(repo => repo.GetKnownUserByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(knownUser);

        _orderRepository.Setup(repo => repo.CreateOrderAsync(It.IsAny<Order>()))
            .ReturnsAsync(createdOrder);

        _kfkaProducer
           .Setup(p => p.ProduceAsync(orderId, It.IsAny<OrderCreatedEvent>()))
           .Returns(Task.CompletedTask);

        var orderService = new OrdersService(_orderRepository.Object, _kfkaProducer.Object, _logger.Object);

        // Act
        var result = await orderService.CreateOrderAsync(newOrderRequest);

        // Assert   
        Assert.NotNull(result);
        Assert.Equal(createdOrder.Id, result.Id);
        _kfkaProducer.Verify(p => p.ProduceAsync(orderId, It.Is<OrderCreatedEvent>(e =>
            e.Id == orderId &&
            e.UserId == createdOrder.UserId &&
            e.Product == createdOrder.Product &&
            e.Price == createdOrder.Price &&
            e.Quantity == createdOrder.Quantity
        )), Times.Once);
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

        var ordersService = new OrdersService(_orderRepository.Object, _kfkaProducer.Object, _logger.Object);

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
        var ordersService = new OrdersService(_orderRepository.Object, _kfkaProducer.Object, _logger.Object);

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

        var orderService = new OrdersService(_orderRepository.Object, _kfkaProducer.Object, _logger.Object);


        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(
            () => orderService.GetOrderByIdAsync(orderId)
        );

        Assert.Equal("Database error", ex.Message);
        _orderRepository.Verify(r => r.GetOrderByIdAsync(It.IsAny<Guid>()), Times.Once);

    }

    [Fact]
    public async Task CreateKnownUserAsync_WhenUserExists_ReturnsExistingUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var knownUser = new KnownUser { UserId = userId };
        var existingUser = new KnownUser { UserId = userId };

        _orderRepository
            .Setup(repo => repo.GetKnownUserByIdAsync(knownUser.UserId))
            .ReturnsAsync(existingUser);
        var orderService = new OrdersService(_orderRepository.Object, _kfkaProducer.Object, _logger.Object);
        // Act
        var result = await orderService.CreateKnownUserAsync(knownUser);

        // Assert
        Assert.Equal(existingUser, result);
        _orderRepository.Verify(repo => repo.CreateKnownUserAsync(It.IsAny<KnownUser>()), Times.Never);
    }

    [Fact]
    public async Task CreateKnownUserAsync_WhenUserDoesNotExist_CreatesAndReturnsNewUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var knownUser = new KnownUser { UserId = userId };
        var createdUser = new KnownUser { UserId = userId };

        _orderRepository
            .Setup(repo => repo.GetKnownUserByIdAsync(knownUser.UserId))
            .ReturnsAsync(null as KnownUser);

        _orderRepository
            .Setup(repo => repo.CreateKnownUserAsync(knownUser))
            .ReturnsAsync(createdUser);
        var orderService = new OrdersService(_orderRepository.Object, _kfkaProducer.Object, _logger.Object);

        // Act
        var result = await orderService.CreateKnownUserAsync(knownUser);

        // Assert
        Assert.Equal(createdUser, result);
        _orderRepository.Verify(repo => repo.CreateKnownUserAsync(knownUser), Times.Once);
    }
}
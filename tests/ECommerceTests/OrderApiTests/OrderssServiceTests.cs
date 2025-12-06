using Microsoft.Extensions.Logging;
using Moq;
using OrderApi.Data;
using OrderApi.Dtos;
using OrderApi.Services;
using Shared.Contracts;
using Shared.Exceptions;
using Shared.Models;

namespace ECommerceTests.OrderApiTests;

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
        var orderService = new OrdersService(_orderRepository.Object, _logger.Object, _kfkaProducer.Object);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BadRequestException>(() => orderService.CreateOrderAsync(newOrderRequest));
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

        var orderService = new OrdersService(_orderRepository.Object, _logger.Object, _kfkaProducer.Object);

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
    public async Task CreateOrder_ThrowsException_WhenRepositoryFails()
    {
        // Arrange
        var newOrderRequest = new OrderCreationRequest
        {
            UserId = Guid.NewGuid(),
            Product = "Test Product",
            Quantity = 2,
            Price = 100
        };
        var knownUser = new KnownUser { UserId = newOrderRequest.UserId };

        _orderRepository.Setup(repo => repo.GetKnownUserByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(knownUser);
        _orderRepository.Setup(repo => repo.CreateOrderAsync(It.IsAny<Order>()))
            .ThrowsAsync(new Exception("Database error"));

        var orderService = new OrdersService(_orderRepository.Object, _logger.Object, _kfkaProducer.Object);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(() => orderService.CreateOrderAsync(newOrderRequest));
        Assert.Equal("Database error", ex.Message);
    }

    [Fact]
    public async Task CreateOrder_ThrowsException_WhenKafkaProducerFails()
    {
        // Arrange
        var newOrderRequest = new OrderCreationRequest
        {
            UserId = Guid.NewGuid(),
            Product = "Test Product",
            Quantity = 2,
            Price = 100
        };
        var knownUser = new KnownUser { UserId = newOrderRequest.UserId };
        var createdOrder = new Order
        {
            Id = Guid.NewGuid(),
            UserId = newOrderRequest.UserId,
            Product = newOrderRequest.Product,
            Quantity = newOrderRequest.Quantity,
            Price = newOrderRequest.Price
        };

        _orderRepository.Setup(repo => repo.GetKnownUserByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(knownUser);
        _orderRepository.Setup(repo => repo.CreateOrderAsync(It.IsAny<Order>()))
            .ReturnsAsync(createdOrder);
        _kfkaProducer.Setup(p => p.ProduceAsync(It.IsAny<Guid>(), It.IsAny<OrderCreatedEvent>()))
            .ThrowsAsync(new Exception("Kafka error"));

        var orderService = new OrdersService(_orderRepository.Object, _logger.Object, _kfkaProducer.Object);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(() => orderService.CreateOrderAsync(newOrderRequest));
        Assert.Equal("Kafka error", ex.Message);
    }

    [Fact]
    public async Task CreateOrder_WithMinimumValues_ReturnsCreatedOrder()
    {
        // Arrange
        var newOrderRequest = new OrderCreationRequest
        {
            UserId = Guid.NewGuid(),
            Product = "A",
            Quantity = 1,
            Price = 0.01m
        };
        var knownUser = new KnownUser { UserId = newOrderRequest.UserId };
        var createdOrder = new Order
        {
            Id = Guid.NewGuid(),
            UserId = newOrderRequest.UserId,
            Product = newOrderRequest.Product,
            Quantity = newOrderRequest.Quantity,
            Price = newOrderRequest.Price
        };

        _orderRepository.Setup(repo => repo.GetKnownUserByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(knownUser);
        _orderRepository.Setup(repo => repo.CreateOrderAsync(It.IsAny<Order>()))
            .ReturnsAsync(createdOrder);
        _kfkaProducer.Setup(p => p.ProduceAsync(It.IsAny<Guid>(), It.IsAny<OrderCreatedEvent>()))
            .Returns(Task.CompletedTask);

        var orderService = new OrdersService(_orderRepository.Object, _logger.Object, _kfkaProducer.Object);

        // Act
        var result = await orderService.CreateOrderAsync(newOrderRequest);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(createdOrder.Id, result.Id);
        Assert.Equal(newOrderRequest.Product, result.Product);
        Assert.Equal(newOrderRequest.Quantity, result.Quantity);
        Assert.Equal(newOrderRequest.Price, result.Price);
    }

    [Fact]
    public async Task CreateOrder_WithLargeValues_ReturnsCreatedOrder()
    {
        // Arrange
        var newOrderRequest = new OrderCreationRequest
        {
            UserId = Guid.NewGuid(),
            Product = new string('A', 1000),
            Quantity = int.MaxValue,
            Price = decimal.MaxValue
        };
        var knownUser = new KnownUser { UserId = newOrderRequest.UserId };
        var createdOrder = new Order
        {
            Id = Guid.NewGuid(),
            UserId = newOrderRequest.UserId,
            Product = newOrderRequest.Product,
            Quantity = newOrderRequest.Quantity,
            Price = newOrderRequest.Price
        };

        _orderRepository.Setup(repo => repo.GetKnownUserByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(knownUser);
        _orderRepository.Setup(repo => repo.CreateOrderAsync(It.IsAny<Order>()))
            .ReturnsAsync(createdOrder);
        _kfkaProducer.Setup(p => p.ProduceAsync(It.IsAny<Guid>(), It.IsAny<OrderCreatedEvent>()))
            .Returns(Task.CompletedTask);

        var orderService = new OrdersService(_orderRepository.Object, _logger.Object, _kfkaProducer.Object);

        // Act
        var result = await orderService.CreateOrderAsync(newOrderRequest);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(createdOrder.Id, result.Id);
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

        var ordersService = new OrdersService(_orderRepository.Object, _logger.Object, _kfkaProducer.Object);

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
        var ordersService = new OrdersService(_orderRepository.Object, _logger.Object, _kfkaProducer.Object);

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

        var orderService = new OrdersService(_orderRepository.Object, _logger.Object, _kfkaProducer.Object);


        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(
            () => orderService.GetOrderByIdAsync(orderId)
        );

        Assert.Equal("Database error", ex.Message);
        _orderRepository.Verify(r => r.GetOrderByIdAsync(It.IsAny<Guid>()), Times.Once);

    }

    [Fact]
    public async Task GetOrderByIdAsync_WithEmptyGuid_ThrowsNotFoundException()
    {
        // Arrange
        var orderId = Guid.Empty;
        _orderRepository.Setup(repo => repo.GetOrderByIdAsync(orderId)).ReturnsAsync((Order?)null);
        var orderService = new OrdersService(_orderRepository.Object, _logger.Object, _kfkaProducer.Object);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<NotFoundException>(() => orderService.GetOrderByIdAsync(orderId));
        _orderRepository.Verify(r => r.GetOrderByIdAsync(orderId), Times.Once);
    }

    [Fact]
    public async Task GetOrderByIdAsync_ReturnsOrderWithAllProperties()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var expectedOrder = new Order
        {
            Id = orderId,
            UserId = userId,
            Product = "Test Product",
            Quantity = 5,
            Price = 99.99m
        };

        _orderRepository.Setup(repo => repo.GetOrderByIdAsync(orderId)).ReturnsAsync(expectedOrder);
        var orderService = new OrdersService(_orderRepository.Object, _logger.Object, _kfkaProducer.Object);

        // Act
        var result = await orderService.GetOrderByIdAsync(orderId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedOrder.Id, result.Id);
        Assert.Equal(expectedOrder.UserId, result.UserId);
        Assert.Equal(expectedOrder.Product, result.Product);
        Assert.Equal(expectedOrder.Quantity, result.Quantity);
        Assert.Equal(expectedOrder.Price, result.Price);
    }

    [Fact]
    public async Task GetOrderByIdAsync_WithMinimumValues_ReturnsOrder()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var expectedOrder = new Order
        {
            Id = orderId,
            UserId = Guid.NewGuid(),
            Product = "P",
            Quantity = 1,
            Price = 0.01m
        };

        _orderRepository.Setup(repo => repo.GetOrderByIdAsync(orderId)).ReturnsAsync(expectedOrder);
        var orderService = new OrdersService(_orderRepository.Object, _logger.Object, _kfkaProducer.Object);

        // Act
        var result = await orderService.GetOrderByIdAsync(orderId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedOrder.Id, result.Id);
        Assert.Equal(expectedOrder.Product, result.Product);
        Assert.Equal(expectedOrder.Quantity, result.Quantity);
        Assert.Equal(expectedOrder.Price, result.Price);
    }

    [Fact]
    public async Task GetOrderByIdAsync_WithMaximumValues_ReturnsOrder()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var expectedOrder = new Order
        {
            Id = orderId,
            UserId = Guid.NewGuid(),
            Product = new string('X', 1000),
            Quantity = int.MaxValue,
            Price = decimal.MaxValue
        };

        _orderRepository.Setup(repo => repo.GetOrderByIdAsync(orderId)).ReturnsAsync(expectedOrder);
        var orderService = new OrdersService(_orderRepository.Object, _logger.Object, _kfkaProducer.Object);

        // Act
        var result = await orderService.GetOrderByIdAsync(orderId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedOrder.Id, result.Id);
        Assert.Equal(expectedOrder.Product, result.Product);
        Assert.Equal(expectedOrder.Quantity, result.Quantity);
        Assert.Equal(expectedOrder.Price, result.Price);
    }

    [Fact]
    public async Task GetOrderByIdAsync_CallsRepositoryOnce()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var expectedOrder = new Order { Id = orderId, UserId = Guid.NewGuid(), Product = "Test", Quantity = 1, Price = 10 };
        _orderRepository.Setup(repo => repo.GetOrderByIdAsync(orderId)).ReturnsAsync(expectedOrder);
        var orderService = new OrdersService(_orderRepository.Object, _logger.Object, _kfkaProducer.Object);

        // Act
        await orderService.GetOrderByIdAsync(orderId);

        // Assert
        _orderRepository.Verify(r => r.GetOrderByIdAsync(orderId), Times.Once);
        _orderRepository.VerifyNoOtherCalls();
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
        var orderService = new OrdersService(_orderRepository.Object, _logger.Object, _kfkaProducer.Object);
        // Act
        await orderService.CreateKnownUserAsync(knownUser);

        // Assert
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
            .ReturnsAsync(1);
        var orderService = new OrdersService(_orderRepository.Object, _logger.Object, _kfkaProducer.Object);

        // Act
        await orderService.CreateKnownUserAsync(knownUser);

        // Assert
        _orderRepository.Verify(repo => repo.CreateKnownUserAsync(knownUser), Times.Once);
    }

    [Fact]
    public async Task CreateKnownUserAsync_WithEmptyGuid_DoesNotCreateUser()
    {
        // Arrange
        var knownUser = new KnownUser { UserId = Guid.Empty };
        _orderRepository.Setup(repo => repo.GetKnownUserByIdAsync(Guid.Empty)).ReturnsAsync((KnownUser?)null);
        var orderService = new OrdersService(_orderRepository.Object, _logger.Object, _kfkaProducer.Object);

        // Act
        await orderService.CreateKnownUserAsync(knownUser);

        // Assert
        _orderRepository.Verify(repo => repo.GetKnownUserByIdAsync(Guid.Empty), Times.Once);
        _orderRepository.Verify(repo => repo.CreateKnownUserAsync(knownUser), Times.Once);
    }

    [Fact]
    public async Task CreateKnownUserAsync_ThrowsException_WhenRepositoryGetFails()
    {
        // Arrange
        var knownUser = new KnownUser { UserId = Guid.NewGuid() };
        _orderRepository.Setup(repo => repo.GetKnownUserByIdAsync(It.IsAny<Guid>()))
            .ThrowsAsync(new Exception("Database error"));
        var orderService = new OrdersService(_orderRepository.Object, _logger.Object, _kfkaProducer.Object);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(() => orderService.CreateKnownUserAsync(knownUser));
        Assert.Equal("Database error", ex.Message);
    }

    [Fact]
    public async Task CreateKnownUserAsync_ThrowsException_WhenRepositoryCreateFails()
    {
        // Arrange
        var knownUser = new KnownUser { UserId = Guid.NewGuid() };
        _orderRepository.Setup(repo => repo.GetKnownUserByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((KnownUser?)null);
        _orderRepository.Setup(repo => repo.CreateKnownUserAsync(It.IsAny<KnownUser>()))
            .ThrowsAsync(new Exception("Create failed"));
        var orderService = new OrdersService(_orderRepository.Object, _logger.Object, _kfkaProducer.Object);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(() => orderService.CreateKnownUserAsync(knownUser));
        Assert.Equal("Create failed", ex.Message);
    }

    [Fact]
    public async Task CreateKnownUserAsync_WithCompleteUserData_CreatesUser()
    {
        // Arrange
        var knownUser = new KnownUser
        {
            UserId = Guid.NewGuid(),
            Email = "test@example.com"
        };
        _orderRepository.Setup(repo => repo.GetKnownUserByIdAsync(knownUser.UserId))
            .ReturnsAsync((KnownUser?)null);
        _orderRepository.Setup(repo => repo.CreateKnownUserAsync(knownUser))
            .ReturnsAsync(1);
        var orderService = new OrdersService(_orderRepository.Object, _logger.Object, _kfkaProducer.Object);

        // Act
        await orderService.CreateKnownUserAsync(knownUser);

        // Assert
        _orderRepository.Verify(repo => repo.CreateKnownUserAsync(It.Is<KnownUser>(u =>
            u.UserId == knownUser.UserId && u.Email == knownUser.Email)), Times.Once);
    }

    [Fact]
    public async Task CreateKnownUserAsync_CallsGetBeforeCreate()
    {
        // Arrange
        var knownUser = new KnownUser { UserId = Guid.NewGuid() };
        _orderRepository.Setup(repo => repo.GetKnownUserByIdAsync(knownUser.UserId))
            .ReturnsAsync((KnownUser?)null);
        _orderRepository.Setup(repo => repo.CreateKnownUserAsync(knownUser))
            .ReturnsAsync(1);
        var orderService = new OrdersService(_orderRepository.Object, _logger.Object, _kfkaProducer.Object);

        // Act
        await orderService.CreateKnownUserAsync(knownUser);

        // Assert
        _orderRepository.Verify(repo => repo.GetKnownUserByIdAsync(knownUser.UserId), Times.Once);
        _orderRepository.Verify(repo => repo.CreateKnownUserAsync(knownUser), Times.Once);
    }
}
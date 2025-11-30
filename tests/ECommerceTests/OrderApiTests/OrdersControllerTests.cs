using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using OrderApi.Controllers;
using OrderApi.Dtos;
using OrderApi.Services;

namespace ECommerceTests.OrderApiTests;

public class OrdersControllerTests
{
    private readonly Mock<IOrdersService> _orderService;
    private readonly Mock<ILogger<OrdersController>> _logger;

    public OrdersControllerTests()
    {
        _orderService = new Mock<IOrdersService>();
        _logger = new Mock<ILogger<OrdersController>>();
    }

    [Fact]
    public async Task GetOrder_ReturnsOrder_WhenOrderExists()
    {
        // Arrange
        Guid orderId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        var expectedOrder = new OrderResponse { Id = orderId, UserId = userId, Product = "Product 1", Quantity = 1, Price = 38m };

        _orderService.Setup(s => s.GetOrderByIdAsync(orderId)).ReturnsAsync(expectedOrder);

        var controller = new OrdersController(_orderService.Object, _logger.Object);

        // Act
        var result = await controller.GetOrder(orderId);

        // Assert
        Assert.NotNull(result);
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(200, okResult.StatusCode);
        var orderResult = Assert.IsType<OrderResponse>(okResult.Value);
        Assert.NotNull(orderResult);
        Assert.Equal(orderId, orderResult.Id);
        Assert.Equal(userId, orderResult.UserId);
        Assert.Equal("Product 1", orderResult.Product);
        Assert.Equal(1, orderResult.Quantity);
        Assert.Equal(38m, orderResult.Price);
    }

    [Fact]
    public async Task GetOrder_Returns_BadRequest_WhenOrderIdEmpty()
    {
        // Arrange
        Guid orderId = Guid.Empty;

        var controller = new OrdersController(_orderService.Object, _logger.Object);

        // Act
        var result = await controller.GetOrder(orderId);

        // Assert
        Assert.NotNull(result);
        var okResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Invalid order ID.", okResult.Value);
    }

    [Fact]
    public async Task GetOrder_LogsWarning_WhenOrderIdEmpty()
    {
        // Arrange
        Guid orderId = Guid.Empty;
        var controller = new OrdersController(_orderService.Object, _logger.Object);
        // Act
        var result = await controller.GetOrder(orderId);
        // Assert
        _logger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("GetOrder called with an empty GUID.")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetOrder_LogsInformation_WhenRetrievingOrder()
    {
        // Arrange
        Guid orderId = Guid.NewGuid();
        var expectedOrder = new OrderResponse { Id = orderId, Product = "Product 1", Quantity = 1, Price = 35m, UserId = Guid.NewGuid() };
        _orderService.Setup(s => s.GetOrderByIdAsync(orderId)).ReturnsAsync(expectedOrder);
        var controller = new OrdersController(_orderService.Object, _logger.Object);
        // Act
        var result = await controller.GetOrder(orderId);
        // Assert
        _logger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Retrieving order with ID: {orderId}")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetOrder_WithValidId_CallsServiceOnce()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var expectedOrder = new OrderResponse { Id = orderId, UserId = Guid.NewGuid() };
        _orderService.Setup(s => s.GetOrderByIdAsync(orderId)).ReturnsAsync(expectedOrder);
        var controller = new OrdersController(_orderService.Object, _logger.Object);

        // Act
        await controller.GetOrder(orderId);

        // Assert
        _orderService.Verify(s => s.GetOrderByIdAsync(orderId), Times.Once);
    }

    [Fact]
    public async Task CreateOrder_ReturnsBadRequest_WhenEmptyFields()
    {
        // Arrange

        var orderCreationRequest = new OrderCreationRequest
        {
            UserId = Guid.Empty,
            Product = "",
            Quantity = 0,
            Price = 0m
        };

        var controller = new OrdersController(_orderService.Object, _logger.Object);

        // Act
        var result = await controller.CreateOrder(orderCreationRequest);

        // Assert
        Assert.NotNull(result);
        var okResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, okResult.StatusCode);
    }

    [Fact]
    public async Task CreateOrder_Success()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var orderCreationRequest = new OrderCreationRequest
        {
            UserId = userId,
            Product = "Product X",
            Quantity = 1,
            Price = 0m
        };
        var orderId = Guid.NewGuid();
        var createdOrder = new OrderResponse
        {
            UserId = orderCreationRequest.UserId,
            Product = orderCreationRequest.Product,
            Quantity = orderCreationRequest.Quantity,
            Price = orderCreationRequest.Price,
            Id = orderId
        };

        _orderService.Setup(s => s.CreateOrderAsync(orderCreationRequest)).ReturnsAsync(createdOrder);
        var controller = new OrdersController(_orderService.Object, _logger.Object);

        // Act
        var result = await controller.CreateOrder(orderCreationRequest);

        // Assert
        Assert.NotNull(result);
        var okResult = Assert.IsType<CreatedAtActionResult>(result);
         Assert.Equal(201, okResult.StatusCode);
        var orderResult = Assert.IsType<OrderResponse>(okResult.Value);
        Assert.NotNull(orderResult);
        Assert.Equal(createdOrder.UserId, orderResult.UserId);
        Assert.Equal(createdOrder.Price, orderResult.Price);
        Assert.Equal(createdOrder.Product, orderResult.Product);
        Assert.Equal(createdOrder.Quantity, orderResult.Quantity);
    }

    [Fact]
    public async Task CreateOrder_ReturnsBadRequest_WhenNullRequest()
    {
        // Arrange
        var newOrder = null as OrderCreationRequest;

        var controller = new OrdersController(_orderService.Object, _logger.Object);

        // Act
        var result = await controller.CreateOrder(newOrder);

        // Assert
        Assert.NotNull(result);
        var errorResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, errorResult.StatusCode);
    }

    [Fact]
    public async Task CreateOrder_ReturnsBadRequest_WhenNegativeQuantity()
    {
        // Arrange
        var orderRequest = new OrderCreationRequest
        {
            UserId = Guid.NewGuid(),
            Product = "Test Product",
            Quantity = -1,
            Price = 10m
        };
        var controller = new OrdersController(_orderService.Object, _logger.Object);

        // Act
        var result = await controller.CreateOrder(orderRequest);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequestResult.StatusCode);
    }

    [Fact]
    public async Task CreateOrder_ReturnsBadRequest_WhenNegativePrice()
    {
        // Arrange
        var orderRequest = new OrderCreationRequest
        {
            UserId = Guid.NewGuid(),
            Product = "Test Product",
            Quantity = 1,
            Price = -10m
        };
        var controller = new OrdersController(_orderService.Object, _logger.Object);

        // Act
        var result = await controller.CreateOrder(orderRequest);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequestResult.StatusCode);
    }

    [Fact]
    public async Task CreateOrder_LogsInformation_WhenCreatingOrder()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var product = "Test Product";
        var orderRequest = new OrderCreationRequest
        {
            UserId = userId,
            Product = product,
            Quantity = 1,
            Price = 10m
        };
        var createdOrder = new OrderResponse { Id = Guid.NewGuid(), UserId = orderRequest.UserId };
        _orderService.Setup(s => s.CreateOrderAsync(orderRequest)).ReturnsAsync(createdOrder);
        var controller = new OrdersController(_orderService.Object, _logger.Object);

        // Act
        await controller.CreateOrder(orderRequest);

        // Assert
        _logger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Creating a new order with UserId: {userId}, Product: {product}")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateOrder_LogsWarning_WhenValidationFails()
    {
        // Arrange
        var orderRequest = new OrderCreationRequest
        {
            UserId = Guid.Empty,
            Product = "",
            Quantity = 0,
            Price = 0m
        };
        var controller = new OrdersController(_orderService.Object, _logger.Object);

        // Act
        await controller.CreateOrder(orderRequest);

        // Assert
        _logger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("CreateOrder called with invalid data")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateOrder_WithMinimumValidValues_ReturnsCreated()
    {
        // Arrange
        var orderRequest = new OrderCreationRequest
        {
            UserId = Guid.NewGuid(),
            Product = "A",
            Quantity = 1,
            Price = 0.01m
        };
        var createdOrder = new OrderResponse { Id = Guid.NewGuid(), UserId = orderRequest.UserId };
        _orderService.Setup(s => s.CreateOrderAsync(orderRequest)).ReturnsAsync(createdOrder);
        var controller = new OrdersController(_orderService.Object, _logger.Object);

        // Act
        var result = await controller.CreateOrder(orderRequest);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(201, createdResult.StatusCode);
    }

    [Fact]
    public async Task CreateOrder_WithMaximumValues_ReturnsCreated()
    {
        // Arrange
        var orderRequest = new OrderCreationRequest
        {
            UserId = Guid.NewGuid(),
            Product = new string('X', 1000),
            Quantity = int.MaxValue,
            Price = decimal.MaxValue
        };
        var createdOrder = new OrderResponse { Id = Guid.NewGuid(), UserId = orderRequest.UserId };
        _orderService.Setup(s => s.CreateOrderAsync(orderRequest)).ReturnsAsync(createdOrder);
        var controller = new OrdersController(_orderService.Object, _logger.Object);

        // Act
        var result = await controller.CreateOrder(orderRequest);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(201, createdResult.StatusCode);
    }

    [Fact]
    public async Task CreateOrder_WithValidRequest_CallsServiceOnce()
    {
        // Arrange
        var orderRequest = new OrderCreationRequest
        {
            UserId = Guid.NewGuid(),
            Product = "Test",
            Quantity = 1,
            Price = 10m
        };
        var createdOrder = new OrderResponse { Id = Guid.NewGuid(), UserId = orderRequest.UserId };
        _orderService.Setup(s => s.CreateOrderAsync(orderRequest)).ReturnsAsync(createdOrder);
        var controller = new OrdersController(_orderService.Object, _logger.Object);

        // Act
        await controller.CreateOrder(orderRequest);

        // Assert
        _orderService.Verify(s => s.CreateOrderAsync(orderRequest), Times.Once);
    }
}

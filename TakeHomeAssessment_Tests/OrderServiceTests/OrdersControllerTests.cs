using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using OrderService.Controllers;
using OrderService.Dtos;
using OrderService.Services;
using Shared.Contracts;
using Shared.Exceptions;

namespace TakeHomeAssessment_Tests.OrderServiceTests;

public class OrdersControllerTests
{
    private readonly Mock<IOrdersService> _orderService;
    private readonly Mock<ILogger<OrdersController>> _logger;
    private readonly Mock<IKafkaProducerWrapper> _kfkaProducer;
    public OrdersControllerTests()
    {
        _orderService = new Mock<IOrdersService>();
        _logger = new Mock<ILogger<OrdersController>>();
        _kfkaProducer = new Mock<IKafkaProducerWrapper>();
    }


    [Fact]
    public async Task GetOrder_ReturnsOrder_WhenOrderExists()
    {
        // Arrange
        Guid orderId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        var expectedOrder = new OrderResponse { Id = orderId, UserId = userId, Product = "Product 1", Quantity = 1, Price = 38m };

        _orderService.Setup(s => s.GetOrderByIdAsync(orderId)).ReturnsAsync(expectedOrder);

        var controller = new OrdersController(_orderService.Object, _logger.Object, _kfkaProducer.Object);

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

        var controller = new OrdersController(_orderService.Object, _logger.Object, _kfkaProducer.Object);

        // Act
        var result = await controller.GetOrder(orderId);

        // Assert
        Assert.NotNull(result);
        var okResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Invalid order ID.", okResult.Value);
    }

    [Fact]
    public async Task GetOrder_Returns_NotFound_WhenOrderDoesNotExist()
    {
        // Arrange
        Guid orderId = Guid.NewGuid();
        _orderService.Setup(s => s.GetOrderByIdAsync(orderId)).ThrowsAsync(new NotFoundException());

        var controller = new OrdersController(_orderService.Object, _logger.Object, _kfkaProducer.Object);

        // Act
        var result = await controller.GetOrder(orderId);

        // Assert
        Assert.NotNull(result);
        var notFoundResult = Assert.IsType<NotFoundResult>(result.Result);
        Assert.Equal(404, notFoundResult.StatusCode);
    }

    [Fact]
    public async Task GetOrder_Returns_InternalServerError_OnException()
    {
        // Arrange
        Guid orderId = Guid.NewGuid();
        _orderService.Setup(s => s.GetOrderByIdAsync(orderId)).ThrowsAsync(new Exception("Database error"));
        var controller = new OrdersController(_orderService.Object, _logger.Object, _kfkaProducer.Object);

        // Act 
        var result = await controller.GetOrder(orderId);

        // Assert
        Assert.NotNull(result);
        var errorResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, errorResult.StatusCode);
    }

    [Fact]
    public async Task GetOrder_LogsWarning_WhenOrderIdEmpty()
    {
        // Arrange
        Guid orderId = Guid.Empty;
        var controller = new OrdersController(_orderService.Object, _logger.Object, _kfkaProducer.Object);
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
        var controller = new OrdersController(_orderService.Object, _logger.Object, _kfkaProducer.Object);
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
    public async Task GetOrder_LogsWarning_WhenOrderNotFound()
    {
        // Arrange
        Guid orderId = Guid.NewGuid();
        var controller = new OrdersController(_orderService.Object, _logger.Object, _kfkaProducer.Object);

        _orderService.Setup(s => s.GetOrderByIdAsync(orderId)).ThrowsAsync(new NotFoundException());

        // Act
        var result = await controller.GetOrder(orderId);

        // Assert
        _logger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Order with ID: {orderId} not found.")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
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

        var controller = new OrdersController(_orderService.Object, _logger.Object, _kfkaProducer.Object);

        // Act
        var result = await controller.CreateOrder(orderCreationRequest);

        // Assert
        Assert.NotNull(result);
        var okResult = Assert.IsType<BadRequestObjectResult>(result.Result);
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

        var createdOrder = new OrderResponse
        {
            UserId = orderCreationRequest.UserId,
            Product = orderCreationRequest.Product,
            Quantity = orderCreationRequest.Quantity,
            Price = orderCreationRequest.Price,
            Id = Guid.NewGuid()
        };

        _orderService.Setup(s => s.CreateOrderAsync(orderCreationRequest)).ReturnsAsync(createdOrder);
        var controller = new OrdersController(_orderService.Object, _logger.Object, _kfkaProducer.Object);

        // Act
        var result = await controller.CreateOrder(orderCreationRequest);

        // Assert
        Assert.NotNull(result);
        var okResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(201, okResult.StatusCode);
        var orderResult = Assert.IsType<OrderResponse>(okResult.Value);
        Assert.NotNull(orderResult);
        Assert.Equal(createdOrder.UserId, orderResult.UserId);
        Assert.Equal(createdOrder.Price, orderResult.Price);
        Assert.Equal(createdOrder.Product, orderResult.Product);
        Assert.Equal(createdOrder.Quantity, orderResult.Quantity);
    }

    [Fact]
    public async Task CreateOrder_Failure()
    {
        // Arrange
        var newOrder = new OrderCreationRequest
        {
            Price = 25m,
            Product = "Product X",
            Quantity = 2,
            UserId = Guid.NewGuid()
        };
        _orderService.Setup(s => s.CreateOrderAsync(newOrder)).ThrowsAsync(new Exception("Database error"));
        var controller = new OrdersController(_orderService.Object, _logger.Object, _kfkaProducer.Object);
        // Act
        var result = await controller.CreateOrder(newOrder);
        // Assert
        Assert.NotNull(result);
        var errorResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, errorResult.StatusCode);
    }
}

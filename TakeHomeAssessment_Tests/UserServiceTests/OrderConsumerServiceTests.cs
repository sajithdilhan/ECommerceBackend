using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Shared.Contracts;
using UserService.Events;

namespace TakeHomeAssessment_Tests.UserServiceTests;

public class OrderConsumerServiceTests
{
    [Fact]
    public void Topic_ShouldReturnConfigValue_WhenConfigured()
    {
        // Arrange
        var configMock = new Mock<IConfiguration>();
        configMock.Setup(c => c["Kafka:ConsumerTopic"]).Returns("order-created");

        var loggerMock = new Mock<ILogger<OrderConsumerService>>();
        var service = new OrderConsumerService(loggerMock.Object, configMock.Object);

        // Assert
        Assert.Equal("order-created", service.Topic);
    }

    [Fact]
    public async Task HandleMessageAsync_ShouldLogInformation()
    {
        // Arrange
        var configMock = new Mock<IConfiguration>();
        configMock.Setup(c => c["Kafka:ConsumerTopic"]).Returns("order-created");

        var loggerMock = new Mock<ILogger<OrderConsumerService>>();
        var service = new OrderConsumerService(loggerMock.Object, configMock.Object);

        var orderId = Guid.NewGuid();
        var @event = new OrderCreatedEvent
        {
            Id = orderId,
            Product = "Test Product",
            Quantity = 2,
            Price = 19.99m,
            UserId = Guid.NewGuid()
        };

        // Act
        await service.HandleMessageAsync(@event);

        // Assert
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"Processed OrderCreated: {orderId}, Test Product")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

}

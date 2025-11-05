using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using OrderService.Events;
using OrderService.Services;
using Shared.Contracts;

namespace TakeHomeAssessment_Tests.OrderServiceTests;

public class UserConsumerServiceTests
{
    private readonly Mock<IConfiguration> _configMock;
    private readonly Mock<IServiceProvider> _serviceProvider;
    private readonly Mock<IOrdersService> _ordersServiceMock;
    private readonly Mock<ILogger<UserConsumerService>> _logger;

    public UserConsumerServiceTests()
    {
        _configMock = new Mock<IConfiguration>();
        _serviceProvider = new Mock<IServiceProvider>();
        _ordersServiceMock = new Mock<IOrdersService>();
        _logger = new Mock<ILogger<UserConsumerService>>();
    }

    [Fact]
    public void Topic_ShouldReturnConfigValue_WhenConfigured()
    {
        // Arrange

        _configMock.Setup(c => c["Kafka:ConsumerTopic"]).Returns("user-created");

        var loggerMock = new Mock<ILogger<UserConsumerService>>();
        var service = new UserConsumerService(loggerMock.Object, _configMock.Object, _serviceProvider.Object);

        // Assert
        Assert.Equal("user-created", service.Topic);
    }
}

using Microsoft.Extensions.Logging;
using Moq;
using Shared.Contracts;
using Shared.Exceptions;
using Shared.Models;
using UserApi.Data;
using UserApi.Dtos;
using UserApi.Services;

namespace ECommerceTests.UserApiTests;

public class UsersServiceTests
{
    private readonly Mock<IUserRepository> _userRepository;
    private readonly Mock<IKafkaProducerWrapper> _kfkaProducer;
    private readonly Mock<ILogger<UsersService>> _logger;
    private readonly CancellationToken _cts = new CancellationToken();

    public UsersServiceTests()
    {
        _userRepository = new Mock<IUserRepository>();
        _kfkaProducer = new Mock<IKafkaProducerWrapper>();
        _logger = new Mock<ILogger<UsersService>>();
    }

    [Fact]
    public async Task GetUserByIdAsync_ReurnsUser_WhenUserExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userRepository.Setup(repo => repo.GetUserByIdAsync(It.IsAny<Guid>(), _cts))
            .ReturnsAsync(new User
            {
                Id = userId,
                Name = "Test User",
                Email = ""
            }
            );

        var usersService = new UsersService(_userRepository.Object, _logger.Object, _kfkaProducer.Object);

        // Act
        var result = await usersService.GetUserByIdAsync(userId, _cts);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.Id);
    }

    [Fact]
    public async Task GetUserByIdAsync_ThrowsNotFoundException_WhenUserDoesNotExist()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        _userRepository.Setup(repo => repo.GetUserByIdAsync(userId, _cts)).ReturnsAsync((User?)null);
        var usersService = new UsersService(_userRepository.Object, _logger.Object, _kfkaProducer.Object);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<NotFoundException>(
            () => usersService.GetUserByIdAsync(userId, _cts)
        );
    }

    [Fact]
    public async Task GetUserByIdAsync_ThrowsException_WhenDb_Exception()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        _userRepository.Setup(repo => repo.GetUserByIdAsync(userId, _cts)).ThrowsAsync(new Exception("Database error"));

        var usersService = new UsersService(_userRepository.Object, _logger.Object, _kfkaProducer.Object);


        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(
            () => usersService.GetUserByIdAsync(userId, _cts)
        );

        Assert.Equal("Database error", ex.Message);
        _userRepository.Verify(r => r.GetUserByIdAsync(It.IsAny<Guid>(), _cts), Times.Once);

    }

    [Fact]
    public async Task CreateUserAsync_ReturnsCreatedUser_WhenValidRequest()
    {
        // Arrange
        var newUserRequest = new UserCreationRequest
        {
            Name = "New User",
            Email = "sajith@mail.com"
        };

        var createdUser = new User
        {
            Id = Guid.NewGuid(),
            Name = newUserRequest.Name,
            Email = newUserRequest.Email
        };

        _userRepository.Setup(repo => repo.GetUserByEmailAsync(newUserRequest.Email, _cts))
            .ReturnsAsync((User?)null);
        _userRepository.Setup(repo => repo.CreateUserAsync(It.IsAny<User>(), _cts))
            .ReturnsAsync(createdUser);

        var usersService = new UsersService(_userRepository.Object, _logger.Object, _kfkaProducer.Object);

        // Act
        var result = await usersService.CreateUserAsync(newUserRequest, _cts);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(createdUser.Id, result.Id);
        Assert.Equal(createdUser.Name, result.Name);
        Assert.Equal(createdUser.Email, result.Email);
        _kfkaProducer.Verify(p => p.ProduceAsync(It.IsAny<Guid>(), It.IsAny<UserCreatedEvent>(), _cts), Times.Once);
    }

    [Fact]
    public async Task CreateUserAsync_ThrowsResourceConflictException_WhenEmailExists()
    {
        // Arrange
        var newUserRequest = new UserCreationRequest
        {
            Name = "New User",
            Email = "sajith@mail.com"
        };

        _userRepository.Setup(repo => repo.GetUserByEmailAsync(newUserRequest.Email, _cts))
            .ReturnsAsync(new User
            {
                Id = Guid.NewGuid(),
                Name = "Existing User",
                Email = newUserRequest.Email
            });

        var usersService = new UsersService(_userRepository.Object, _logger.Object, _kfkaProducer.Object);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ResourceConflictException>(
           () => usersService.CreateUserAsync(newUserRequest, _cts)
       );

        Assert.Contains("already exists", ex.Message);
        _userRepository.Verify(r => r.CreateUserAsync(It.IsAny<User>(), _cts), Times.Never);
        _kfkaProducer.Verify(p => p.ProduceAsync(It.IsAny<Guid>(), It.IsAny<UserCreatedEvent>(), _cts), Times.Never);
    }

    [Fact]
    public async Task CreateUserAsync_ThrowsException_WhenRepositoryReturnsNull()
    {
        // Arrange
        var newUserRequest = new UserCreationRequest
        {
            Name = "New User",
            Email = "sajith@mail.com"
        };

        _userRepository.Setup(repo => repo.GetUserByEmailAsync(newUserRequest.Email, _cts))
            .ReturnsAsync((User?)null);
        _userRepository.Setup(repo => repo.CreateUserAsync(It.IsAny<User>(), _cts))
            .ReturnsAsync((User?)null);

        var usersService = new UsersService(_userRepository.Object, _logger.Object, _kfkaProducer.Object);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(
            () => usersService.CreateUserAsync(newUserRequest, _cts)
        );

        Assert.Equal("Failed to create user.", ex.Message);
        _kfkaProducer.Verify(p => p.ProduceAsync(It.IsAny<Guid>(), It.IsAny<UserCreatedEvent>(), _cts), Times.Never);
    }

    [Fact]
    public async Task CreateUserAsync_ThrowsException_WhenRepositoryThrowsException()
    {
        // Arrange
        var newUserRequest = new UserCreationRequest
        {
            Name = "New User",
            Email = "sajith@mail.com"
        };

        _userRepository.Setup(repo => repo.GetUserByEmailAsync(newUserRequest.Email, _cts))
            .ThrowsAsync(new Exception("Database connection failed"));

        var usersService = new UsersService(_userRepository.Object, _logger.Object, _kfkaProducer.Object);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(
            () => usersService.CreateUserAsync(newUserRequest, _cts)
        );

        Assert.Equal("Database connection failed", ex.Message);
        _userRepository.Verify(r => r.CreateUserAsync(It.IsAny<User>(), _cts), Times.Never);
        _kfkaProducer.Verify(p => p.ProduceAsync(It.IsAny<Guid>(), It.IsAny<UserCreatedEvent>(), _cts), Times.Never);
    }

    [Fact]
    public async Task CreateUserAsync_ThrowsException_WhenKafkaProducerFails()
    {
        // Arrange
        var newUserRequest = new UserCreationRequest
        {
            Name = "New User",
            Email = "sajith@mail.com"
        };

        var createdUser = new User
        {
            Id = Guid.NewGuid(),
            Name = newUserRequest.Name,
            Email = newUserRequest.Email
        };

        _userRepository.Setup(repo => repo.GetUserByEmailAsync(newUserRequest.Email, _cts))
            .ReturnsAsync((User?)null);
        _userRepository.Setup(repo => repo.CreateUserAsync(It.IsAny<User>(), _cts))
            .ReturnsAsync(createdUser);
        _kfkaProducer.Setup(p => p.ProduceAsync(It.IsAny<Guid>(), It.IsAny<UserCreatedEvent>(), _cts))
            .ThrowsAsync(new Exception("Kafka connection failed"));

        var usersService = new UsersService(_userRepository.Object, _logger.Object, _kfkaProducer.Object);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(
            () => usersService.CreateUserAsync(newUserRequest, _cts)
        );

        Assert.Equal("Kafka connection failed", ex.Message);
        _userRepository.Verify(r => r.CreateUserAsync(It.IsAny<User>(), _cts), Times.Once);
    }
}
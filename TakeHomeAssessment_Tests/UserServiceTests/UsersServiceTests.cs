using Moq;
using Shared.Contracts;
using Shared.Exceptions;
using Shared.Models;
using UserService.Data;
using UserService.Dtos;
using UserService.Services;

namespace TakeHomeAssessment_Tests.UserServiceTests;

public class UsersServiceTests
{
    private readonly Mock<IUserRepository> _userRepository;
    private readonly Mock<IKafkaProducerWrapper> _kfkaProducer;

    public UsersServiceTests()
    {
        _userRepository = new Mock<IUserRepository>();
        _kfkaProducer = new Mock<IKafkaProducerWrapper>();
    }

    [Fact]
    public async Task GetUserByIdAsync_ReurnsUser_WhenUserExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userRepository.Setup(repo => repo.GetUserByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new User
            {
                Id = userId,
                Name = "Test User",
                Email = ""
            }
            );

        var usersService = new UsersService(_userRepository.Object, _kfkaProducer.Object);

        // Act
        var result = await usersService.GetUserByIdAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.Id);
    }

    [Fact]
    public async Task GetUserByIdAsync_ThrowsNotFoundException_WhenUserDoesNotExist()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        _userRepository.Setup(repo => repo.GetUserByIdAsync(userId)).ReturnsAsync((User?)null);
        var usersService = new UsersService(_userRepository.Object, _kfkaProducer.Object);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<NotFoundException>(
            () => usersService.GetUserByIdAsync(userId)
        );
    }

    [Fact]
    public async Task GetUserByIdAsync_ThrowsException_WhenDb_Exception()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        _userRepository.Setup(repo => repo.GetUserByIdAsync(userId)).ThrowsAsync(new Exception("Database error"));

        var usersService = new UsersService(_userRepository.Object, _kfkaProducer.Object);


        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(
            () => usersService.GetUserByIdAsync(userId)
        );

        Assert.Equal("Database error", ex.Message);
        _userRepository.Verify(r => r.GetUserByIdAsync(It.IsAny<Guid>()), Times.Once);

    }

    [Fact]
    public async Task CreateUser_Returns_CreatedUser()
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

        _userRepository.Setup(repo => repo.CreateUserAsync(It.IsAny<User>()))
            .ReturnsAsync(createdUser);

    }

    [Fact]
    public async Task CreateUser_WithSameEmail_Returns_ConflictRequest()
    {
        // Arrange
        var newUserRequest = new UserCreationRequest
        {
            Name = "New User",
            Email = "sajith@mail.com"
        };

        _userRepository.Setup(repo => repo.GetUserByEmailAsync(newUserRequest.Email))
            .ReturnsAsync(new User
            {
                Id = Guid.NewGuid(),
                Name = "Existing User",
                Email = newUserRequest.Email
            });

        var usersService = new UsersService(_userRepository.Object, _kfkaProducer.Object);

        //Act & Assert
        var ex = await Assert.ThrowsAsync<ResourceConflictException>(
           () => usersService.CreateUserAsync(newUserRequest)
       );
    }
}

using Moq;
using Shared.Exceptions;
using Shared.Models;
using UserService.Data;
using UserService.Dtos;
using UserService.Services;

namespace TakeHomeAssessment_Tests.UserServiceTests;

public class UsersServiceTests
{
    private readonly Mock<IUserRepository> _userRepository;

    public UsersServiceTests()
    {
        _userRepository = new Mock<IUserRepository>();
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

        var usersService = new UsersService(_userRepository.Object);

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
        var usersService = new UsersService(_userRepository.Object);

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

        var usersService = new UsersService(_userRepository.Object);


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
}

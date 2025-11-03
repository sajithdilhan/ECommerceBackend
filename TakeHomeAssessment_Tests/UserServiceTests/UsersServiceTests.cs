using Microsoft.Extensions.Logging;
using Moq;
using Shared.Exceptions;
using Shared.Models;
using UserService.Data;
using UserService.Services;

namespace TakeHomeAssessment_Tests.UserServiceTests;

public class UsersServiceTests
{
    private readonly Mock<IUserRepository> _userRepository;
    private readonly Mock<ILogger<UsersService>> _logger;

    public UsersServiceTests()
    {
        _userRepository = new Mock<IUserRepository>();
        _logger = new Mock<ILogger<UsersService>>();
    }

    [Fact]
    public async Task UsersService_ReurnsUser_WhenUserExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userRepository.Setup(repo => repo.GetUserByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new User
            {
                Id = userId,
                Name = "Test User",
                Email = ""}
            );

        var usersService = new UsersService(_userRepository.Object, _logger.Object);

        // Act
        var result = await usersService.GetUserByIdAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.Id);
    }

    [Fact]
    public async Task UsersService_ThrowsNotFoundException_WhenUserDoesNotExist()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        _userRepository.Setup(repo => repo.GetUserByIdAsync(userId)).ReturnsAsync((User?)null);
        var usersService = new UsersService(_userRepository.Object, _logger.Object);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<NotFoundException>(
            () => usersService.GetUserByIdAsync(userId)
        );
    }

    [Fact]
    public async Task UsersService_ThrowsException_WhenDb_Exception()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        _userRepository.Setup(repo => repo.GetUserByIdAsync(userId)).ThrowsAsync(new Exception("Database error"));

        var usersService = new UsersService(_userRepository.Object, _logger.Object);


        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(
            () => usersService.GetUserByIdAsync(userId)
        );

        Assert.Equal("Database error", ex.Message);
        _userRepository.Verify(r => r.GetUserByIdAsync(It.IsAny<Guid>()), Times.Once);

    }

}

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Shared.Contracts;
using Shared.Exceptions;
using UserService.Controllers;
using UserService.Dtos;
using UserService.Services;

namespace TakeHomeAssessment_Tests.UserServiceTests;

public class UsersControllerTests
{
    private readonly Mock<IUsersService> _userService;
    private readonly Mock<ILogger<UsersController>> _logger;

    public UsersControllerTests()
    {
        _userService = new Mock<IUsersService>();
        _logger = new Mock<ILogger<UsersController>>();
    }

    [Fact]
    public async Task GetUser_ReturnsUser_WhenUserExists()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        var expectedUser = new UserResponse { Id = userId, Name = "John Doe", Email = "test@test.com" };

        _userService.Setup(s => s.GetUserByIdAsync(userId)).ReturnsAsync(expectedUser);

        var controller = new UsersController(_userService.Object, _logger.Object);

        // Act
        var result = await controller.GetUser(userId);

        // Assert
        Assert.NotNull(result);
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(200, okResult.StatusCode);
        var userResult = Assert.IsType<UserResponse>(okResult.Value);
        Assert.NotNull(userResult);
        Assert.Equal(userId, userResult.Id);
        Assert.Equal("John Doe", userResult.Name);
        Assert.Equal("test@test.com", userResult.Email);
    }

    [Fact]
    public async Task GetUser_Returns_BadRequest_WhenUserIdEmpty()
    {
        // Arrange
        Guid userId = Guid.Empty;

        var controller = new UsersController(_userService.Object, _logger.Object);

        // Act
        var result = await controller.GetUser(userId);

        // Assert
        Assert.NotNull(result);
        var okResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Invalid user ID.", okResult.Value);
    }

    [Fact]
    public async Task GetUser_Returns_NotFound_WhenUserDoesNotExist()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        _userService.Setup(s => s.GetUserByIdAsync(userId)).ThrowsAsync(new NotFoundException());

        var controller = new UsersController(_userService.Object, _logger.Object);

        // Act
        var result = await controller.GetUser(userId);

        // Assert
        Assert.NotNull(result);
        var notFoundResult = Assert.IsType<NotFoundResult>(result.Result);
        Assert.Equal(404, notFoundResult.StatusCode);
    }

    [Fact]
    public async Task GetUser_Returns_InternalServerError_OnException()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        _userService.Setup(s => s.GetUserByIdAsync(userId)).ThrowsAsync(new Exception("Database error"));
        var controller = new UsersController(_userService.Object, _logger.Object);

        // Act 
        var result = await controller.GetUser(userId);

        // Assert
        Assert.NotNull(result);
        var errorResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, errorResult.StatusCode);
    }

    [Fact]
    public async Task GetUser_LogsWarning_WhenUserIdEmpty()
    {
        // Arrange
        Guid userId = Guid.Empty;
        var controller = new UsersController(_userService.Object, _logger.Object);
        // Act
        var result = await controller.GetUser(userId);
        // Assert
        _logger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("GetUser called with an empty GUID.")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetUser_LogsInformation_WhenRetrievingUser()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        var expectedUser = new UserResponse { Id = userId, Name = "John Doe", Email = "sajith@mail.com" };
        _userService.Setup(s => s.GetUserByIdAsync(userId)).ReturnsAsync(expectedUser);
        var controller = new UsersController(_userService.Object, _logger.Object);
        // Act
        var result = await controller.GetUser(userId);
        // Assert
        _logger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Retrieving user with ID: {userId}")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetUser_LogsWarning_WhenUserNotFound()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        var controller = new UsersController(_userService.Object, _logger.Object);

        _userService.Setup(s => s.GetUserByIdAsync(userId)).ThrowsAsync(new NotFoundException());

        // Act
        var result = await controller.GetUser(userId);

        // Assert
        _logger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"User with ID: {userId} not found.")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }


    [Fact]
    public async Task CreateUser_ReturnsBadRequest_WhenNameOrEmailEmpty()
    {
        // Arrange
        var userCreationRequest = new UserCreationRequest
        {
            Name = "",
            Email = ""
        };

        var controller = new UsersController(_userService.Object, _logger.Object);

        // Act
        var result = await controller.CreateUser(userCreationRequest);

        // Assert
        Assert.NotNull(result);
        var okResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(400, okResult.StatusCode);
    }

    [Fact]
    public async Task CreateUser_Success()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var newUser = new UserCreationRequest
        {
            Name = "New User",
            Email = "sajith@mail.com"
        };

        var createdUser = new UserResponse
        {
            Id = userId,
            Name = newUser.Name,
            Email = newUser.Email
        };

        _userService.Setup(s => s.CreateUserAsync(newUser)).ReturnsAsync(createdUser);
        var controller = new UsersController(_userService.Object, _logger.Object);

        // Act
        var result = await controller.CreateUser(newUser);

        // Assert
        Assert.NotNull(result);
        var okResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(201, okResult.StatusCode);
        var userResult = Assert.IsType<UserResponse>(okResult.Value);
        Assert.NotNull(userResult);
        Assert.Equal(newUser.Name, userResult.Name);
        Assert.Equal(newUser.Email, userResult.Email);
    }

    [Fact]
    public async Task CreateUser_ReturnsError_WhenDbFail()
    {
        // Arrange
        var newUser = new UserCreationRequest
        {
            Name = "New User",
            Email = "sajith@mail.com"
        };
        _userService.Setup(s => s.CreateUserAsync(newUser)).ThrowsAsync(new Exception("Database error"));
        var controller = new UsersController(_userService.Object, _logger.Object);

        // Act
        var result = await controller.CreateUser(newUser);

        // Assert
        Assert.NotNull(result);
        var errorResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, errorResult.StatusCode);
    }

    [Fact]
    public async Task CreateUser_ReturnsError_WhenEmailAlreadyExists()
    {
        // Arrange
        var newUser = new UserCreationRequest
        {
            Name = "New User",
            Email = "sajith@mail.com"
        };

        _userService.Setup(s => s.CreateUserAsync(newUser)).ThrowsAsync(new ResourceConflictException("User with the same email address already existing"));
        var controller = new UsersController(_userService.Object, _logger.Object);

        // Act
        var result = await controller.CreateUser(newUser);

        // Assert
        Assert.NotNull(result);
        var errorResult = Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Equal(409, errorResult.StatusCode);
    }
}
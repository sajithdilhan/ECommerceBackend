using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;
using UserApi.Controllers;
using UserApi.Dtos;
using UserApi.Services;

namespace ECommerceTests.UserApiTests;

public class UsersControllerTests
{
    private readonly Mock<IUsersService> _userService;
    private readonly Mock<ILogger<UsersController>> _logger;
    private readonly Mock<IDistributedCache> _cach;

    public UsersControllerTests()
    {
        _userService = new Mock<IUsersService>();
        _logger = new Mock<ILogger<UsersController>>();
        _cach = new Mock<IDistributedCache>();
    }

    [Fact]
    public async Task GetUser_ReturnsUser_WhenUserExists()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        var expectedUser = new UserResponse { Id = userId, Name = "John Doe", Email = "test@test.com" };

        _userService.Setup(s => s.GetUserByIdAsync(userId)).ReturnsAsync(expectedUser);

        var controller = new UsersController(_userService.Object, _logger.Object, _cach.Object);

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

        var controller = new UsersController(_userService.Object, _logger.Object, _cach.Object);

        // Act
        var result = await controller.GetUser(userId);

        // Assert
        Assert.NotNull(result);
        var okResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Invalid user ID.", okResult.Value);
    }

    [Fact]
    public async Task GetUser_LogsWarning_WhenUserIdEmpty()
    {
        // Arrange
        Guid userId = Guid.Empty;
        var controller = new UsersController(_userService.Object, _logger.Object, _cach.Object);
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
        var controller = new UsersController(_userService.Object, _logger.Object, _cach.Object);
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
    public async Task CreateUser_ReturnsBadRequest_WhenNameOrEmailEmpty()
    {
        // Arrange
        var userCreationRequest = new UserCreationRequest
        {
            Name = "",
            Email = ""
        };

        var controller = new UsersController(_userService.Object, _logger.Object, _cach.Object);

        // Act
        var result = await controller.CreateUser(userCreationRequest);

        // Assert
        Assert.NotNull(result);
        var okResult = Assert.IsType<BadRequestObjectResult>(result);
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
        var controller = new UsersController(_userService.Object, _logger.Object, _cach.Object);

        // Act
        var result = await controller.CreateUser(newUser);

        // Assert
        Assert.NotNull(result);
        var okResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(201, okResult.StatusCode);
        var userResult = Assert.IsType<UserResponse>(okResult.Value);
        Assert.NotNull(userResult);
        Assert.Equal(newUser.Name, userResult.Name);
        Assert.Equal(newUser.Email, userResult.Email);
    }

    [Fact]
    public async Task CreateUser_ReturnsBadRequest_WhenNullRequest()
    {
        // Arrange
        var controller = new UsersController(_userService.Object, _logger.Object, _cach.Object);

        // Act
        var result = await controller.CreateUser(null);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequestResult.StatusCode);
    }

    [Fact]
    public async Task CreateUser_ReturnsBadRequest_WhenNameIsNull()
    {
        // Arrange
        var userRequest = new UserCreationRequest
        {
            Name = null,
            Email = "test@example.com"
        };
        var controller = new UsersController(_userService.Object, _logger.Object, _cach.Object);

        // Act
        var result = await controller.CreateUser(userRequest);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequestResult.StatusCode);
    }

    [Fact]
    public async Task CreateUser_ReturnsBadRequest_WhenEmailIsNull()
    {
        // Arrange
        var userRequest = new UserCreationRequest
        {
            Name = "Test User",
            Email = null
        };
        var controller = new UsersController(_userService.Object, _logger.Object, _cach.Object);

        // Act
        var result = await controller.CreateUser(userRequest);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequestResult.StatusCode);
    }

    [Fact]
    public async Task CreateUser_ReturnsBadRequest_WhenEmailIsInvalid()
    {
        // Arrange
        var userRequest = new UserCreationRequest
        {
            Name = "Test User",
            Email = "TEst Email"
        };
        var controller = new UsersController(_userService.Object, _logger.Object, _cach.Object);

        // Act
        var result = await controller.CreateUser(userRequest);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequestResult.StatusCode);
    }

    [Fact]
    public async Task CreateUser_ReturnsBadRequest_WhenNameIsWhitespace()
    {
        // Arrange
        var userRequest = new UserCreationRequest
        {
            Name = "   ",
            Email = "test@example.com"
        };
        var controller = new UsersController(_userService.Object, _logger.Object, _cach.Object);

        // Act
        var result = await controller.CreateUser(userRequest);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequestResult.StatusCode);
    }

    [Fact]
    public async Task CreateUser_ReturnsBadRequest_WhenEmailIsWhitespace()
    {
        // Arrange
        var userRequest = new UserCreationRequest
        {
            Name = "Test User",
            Email = "   "
        };
        var controller = new UsersController(_userService.Object, _logger.Object, _cach.Object);

        // Act
        var result = await controller.CreateUser(userRequest);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequestResult.StatusCode);
    }

    [Fact]
    public async Task CreateUser_LogsInformation_WhenCreatingUser()
    {
        // Arrange
        var userRequest = new UserCreationRequest
        {
            Name = "Test User",
            Email = "sajith@mail.com"
        };
        var createdUser = new UserResponse { Id = Guid.NewGuid(), Name = userRequest.Name, Email = userRequest.Email };
        _userService.Setup(s => s.CreateUserAsync(userRequest)).ReturnsAsync(createdUser);
        var controller = new UsersController(_userService.Object, _logger.Object, _cach.Object);

        // Act
        await controller.CreateUser(userRequest);

        // Assert
        _logger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Creating a new user with Name: {userRequest.Name}, Email: {userRequest.Email}")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateUser_LogsWarning_WhenValidationFails()
    {
        // Arrange
        var userRequest = new UserCreationRequest
        {
            Name = "",
            Email = ""
        };
        var controller = new UsersController(_userService.Object, _logger.Object, _cach.Object);

        // Act
        await controller.CreateUser(userRequest);

        // Assert
        _logger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("CreateUser called with invalid data")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateUser_WithMinimumValidValues_ReturnsCreated()
    {
        // Arrange
        var userRequest = new UserCreationRequest
        {
            Name = "A",
            Email = "a@b.c"
        };
        var createdUser = new UserResponse { Id = Guid.NewGuid(), Name = userRequest.Name, Email = userRequest.Email };
        _userService.Setup(s => s.CreateUserAsync(userRequest)).ReturnsAsync(createdUser);
        var controller = new UsersController(_userService.Object, _logger.Object, _cach.Object);

        // Act
        var result = await controller.CreateUser(userRequest);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(201, createdResult.StatusCode);
    }

    [Fact]
    public async Task CreateUser_WithLongValues_ReturnsCreated()
    {
        // Arrange
        var userRequest = new UserCreationRequest
        {
            Name = new string('A', 1000),
            Email = new string('a', 100) + "@example.com"
        };
        var createdUser = new UserResponse { Id = Guid.NewGuid(), Name = userRequest.Name, Email = userRequest.Email };
        _userService.Setup(s => s.CreateUserAsync(userRequest)).ReturnsAsync(createdUser);
        var controller = new UsersController(_userService.Object, _logger.Object, _cach.Object);

        // Act
        var result = await controller.CreateUser(userRequest);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(201, createdResult.StatusCode);
    }

    [Fact]
    public async Task CreateUser_CallsServiceOnce()
    {
        // Arrange
        var userRequest = new UserCreationRequest
        {
            Name = "Test User",
            Email = "sajith@mail.com"
        };
        var createdUser = new UserResponse { Id = Guid.NewGuid(), Name = userRequest.Name, Email = userRequest.Email };
        _userService.Setup(s => s.CreateUserAsync(userRequest)).ReturnsAsync(createdUser);
        var controller = new UsersController(_userService.Object, _logger.Object, _cach.Object);

        // Act
        await controller.CreateUser(userRequest);

        // Assert
        _userService.Verify(s => s.CreateUserAsync(userRequest), Times.Once);
    }
    
}
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using UserService.Controllers;
using UserService.Data;
using UserService.Models;

namespace TakeHomeAssessment_Tests;

public class UsersControllerTests
{
    private readonly UserDbContext _dbContext;

    public UsersControllerTests()
    {
        var options = new DbContextOptionsBuilder<UserDbContext>()
           .UseInMemoryDatabase(databaseName: "UserDatabase")
           .Options;
        _dbContext = new UserDbContext(options);
    }

    [Fact]
    public void GetUser_ReturnsUser_WhenUserExists()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        var expectedUser = new User { Id = userId, Name = "John Doe", Email = "test@test.com" };

        var controller = new UsersController(_dbContext);

        // Act
        var result = controller.GetUser(userId);

        // Assert
        Assert.NotNull(result);
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var userResult = Assert.IsType<User>(okResult.Value);
        Assert.NotNull(userResult);
        Assert.Equal(userId, userResult.Id);
        Assert.Equal("John Doe", userResult.Name); 
        Assert.Equal("test@test.com", userResult.Email);
    }

    [Fact]
    public void GetUser_Returns_BadRequest_WhenUserIdEmpty()
    {
        // Arrange
        Guid userId = Guid.Empty;
        

        var controller = new UsersController(_dbContext);

        // Act
        var result = controller.GetUser(userId);

        // Assert
        Assert.NotNull(result);
        var okResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Invalid user ID.", okResult.Value);
    }
}

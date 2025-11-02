using Microsoft.AspNetCore.Mvc;
using UserService.Controllers;
using UserService.Models;

namespace TakeHomeAssessment_Tests;

public class UsersControllerTests
{
    [Fact]
    public void GetUser_ReturnsUser_WhenUserExists()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        var expectedUser = new User { Id = userId, Name = "John Doe", Email = "test@test.com" };

        var controller = new UsersController();

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
}

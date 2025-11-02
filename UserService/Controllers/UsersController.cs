using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using UserService.Models;

namespace UserService.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsersController : ControllerBase
{
    [HttpGet("{id}")]
    public ActionResult<User> GetUser(Guid id)
    {
        // For demonstration purposes, returning a dummy user
        var user = new User { Id = id, Name = "John Doe", Email = "test@test.com" };
        return Ok(user);
    }
}

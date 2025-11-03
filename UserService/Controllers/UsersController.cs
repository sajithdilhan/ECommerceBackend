using Microsoft.AspNetCore.Mvc;
using UserService.Data;
using UserService.Models;

namespace UserService.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsersController : ControllerBase
{
    private readonly UserDbContext _context;
    public UsersController(UserDbContext userDbContext)
    {
            _context = userDbContext;
    }

    [HttpGet("{id}")]
    public ActionResult<User> GetUser(Guid id)
    {

        if (id == Guid.Empty)
        {
            return BadRequest("Invalid user ID.");
        }
        
        var user = _context.Users.Find(id);

        return Ok(user);
    }
}

using Microsoft.AspNetCore.Mvc;
using System.Net.Mail;
using UserApi.Dtos;
using UserApi.Services;

namespace UserApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsersController : ControllerBase
{
    private readonly IUsersService _usersService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUsersService usersService, ILogger<UsersController> logger)
    {
        _usersService = usersService;
        _logger = logger;
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(UserResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<UserResponse>> GetUser(Guid id)
    {
        if (id == Guid.Empty)
        {
            _logger.LogWarning("GetUser called with an empty GUID.");
            return BadRequest("Invalid user ID.");
        }

        _logger.LogInformation("Retrieving user with ID: {UserId}", id);
        var user = await _usersService.GetUserByIdAsync(id);

        return Ok(user);
    }

    [HttpPost]
    [ProducesResponseType(typeof(UserResponse), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(409)]
    [ProducesResponseType(500)]
    public async Task<ActionResult> CreateUser(UserCreationRequest newUser)
    {
        if (IsInValidRequest(newUser))
        {
            _logger.LogWarning("CreateUser called with invalid data.");
            return BadRequest("Invalid request data.");
        }

        _logger.LogInformation("Creating a new user with Name: {UserName}, Email: {UserEmail}", newUser.Name, newUser.Email);
        var createdUser = await _usersService.CreateUserAsync(newUser);

        return CreatedAtAction(nameof(GetUser), new { id = createdUser.Id }, createdUser);
    }

    private static bool IsInValidRequest(UserCreationRequest newUser)
    {
        return newUser is null 
            || string.IsNullOrWhiteSpace(newUser?.Name) 
            || string.IsNullOrWhiteSpace(newUser?.Email)
            || !IsValidEmailFormat(newUser?.Email);
    }

    private static bool IsValidEmailFormat(string? email)
    {
        try
        {
            if(string.IsNullOrWhiteSpace(email)) 
                return false;

            var addr = new MailAddress(email);
            return addr.Address == email;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
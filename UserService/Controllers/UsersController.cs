using Microsoft.AspNetCore.Mvc;
using Shared.Exceptions;
using UserService.Dtos;
using UserService.Services;

namespace UserService.Controllers;

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
        try
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
        catch (NotFoundException)
        {
            _logger.LogWarning("User with ID: {UserId} not found.", id);
            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving user with ID: {UserId}", id);
            return StatusCode(500, "An error occurred while processing your request.");
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(UserResponse), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(409)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<UserResponse>> CreateUser(UserCreationRequest newUser)
    {
        try
        {
            if (IsValidRequest(newUser))
            {
                _logger.LogWarning("CreateUser called with invalid data.");
                return BadRequest("Invalid request data.");
            }

            _logger.LogInformation("Creating a new user with Name: {UserName}, Email: {UserEmail}", newUser.Name, newUser.Email);
            var createdUser = await _usersService.CreateUserAsync(newUser);
            return CreatedAtAction(nameof(GetUser), new { id = createdUser.Id }, createdUser);

        }
        catch (ResourceConflictException ex)
        {
            _logger.LogWarning(ex, "Conflict occurred while creating user with Email: {UserEmail}", newUser.Email);
            return Conflict("User with the same email already existing");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while creating user");
            return StatusCode(500, "An error occurred while processing your request.");
        }
    }

    private static bool IsValidRequest(UserCreationRequest newUser)
    {
        return newUser is null || string.IsNullOrWhiteSpace(newUser?.Name) || string.IsNullOrWhiteSpace(newUser?.Email);
    }
}
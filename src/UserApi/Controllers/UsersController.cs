using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Shared.Common;
using System.Net.Mail;
using System.Text.Json;
using UserApi.Dtos;
using UserApi.Services;

namespace UserApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsersController(IUsersService usersService, ILogger<UsersController> logger, IDistributedCache cache) : ControllerBase
{
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(UserResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<UserResponse>> GetUser(Guid id)
    {
        if (id == Guid.Empty)
        {
            logger.LogWarning("GetUser called with an empty GUID.");
            return BadRequest("Invalid user ID.");
        }

        logger.LogInformation("Retrieving user with ID: {UserId}", id);

        var cached = await cache.GetStringAsync($"{Constants.CacheKeyUserPrefix}{id}");
        if (cached != null)
        {
            return Ok(JsonSerializer.Deserialize<UserResponse>(cached));
        }

        var user = await usersService.GetUserByIdAsync(id);

        await cache.SetStringAsync(
            $"{Constants.CacheKeyUserPrefix}{id}",
            JsonSerializer.Serialize(user),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            });

        return Ok(user);
    }

    [HttpPost]
    [ProducesResponseType(typeof(UserResponse), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(409)]
    [ProducesResponseType(500)]
    public async Task<ActionResult> CreateUser(UserCreationRequest newUser)
    {
        if (IsInValidRequest(newUser))
        {
            logger.LogWarning("CreateUser called with invalid data.");
            return BadRequest("Invalid request data.");
        }

        logger.LogInformation("Creating a new user {@user}", JsonSerializer.Serialize(newUser));
        var createdUser = await usersService.CreateUserAsync(newUser);

        await cache.SetStringAsync(
            $"{Constants.CacheKeyUserPrefix}{createdUser.Id}",
            JsonSerializer.Serialize(createdUser),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            });

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
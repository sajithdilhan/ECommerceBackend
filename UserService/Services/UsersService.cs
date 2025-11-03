using Shared.Exceptions;
using Shared.Models;
using UserService.Data;
using UserService.Dtos;

namespace UserService.Services;

public class UsersService : IUsersService
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<UsersService> _logger;

    public UsersService(IUserRepository userRepository, ILogger<UsersService> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public Task<UserResponse> CreateUserAsync(UserCreationRequest newUser)
    {
        throw new NotImplementedException();
    }

    public async Task<UserResponse> GetUserByIdAsync(Guid id)
    {
        var response = await _userRepository.GetUserByIdAsync(id);
        return response == null ? throw new NotFoundException($"User with ID {id} not found.") : UserResponse.FromUser(response);
    }
}

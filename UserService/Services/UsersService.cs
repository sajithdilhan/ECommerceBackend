using Shared.Exceptions;
using UserService.Data;
using UserService.Dtos;

namespace UserService.Services;

public class UsersService : IUsersService
{
    private readonly IUserRepository _userRepository;

    public UsersService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<UserResponse> CreateUserAsync(UserCreationRequest newUser)
    {
        var user = newUser.MapToUser();

        var exsistingUser = await _userRepository.GetUserByEmailAsync(user.Email);
        if (exsistingUser != null)
        {
            throw new ResourceConflictException($"User with email {user.Email} already exists.");
        }

        return await _userRepository.CreateUserAsync(user)
            .ContinueWith(task => UserResponse.MapUserToResponseDto(task.Result));
    }

    public async Task<UserResponse> GetUserByIdAsync(Guid id)
    {
        var response = await _userRepository.GetUserByIdAsync(id);
        return response == null ? throw new NotFoundException($"User with ID {id} not found.") : UserResponse.MapUserToResponseDto(response);
    }
}

using UserService.Dtos;

namespace UserService.Services;

public interface IUsersService
{
    public Task<UserResponse> CreateUserAsync(UserCreationRequest newUser);
    public Task<UserResponse> GetUserByIdAsync(Guid id);
}

using UserApi.Dtos;

namespace UserApi.Services;

public interface IUsersService
{
    public Task<UserResponse> CreateUserAsync(UserCreationRequest newUser);
    public Task<UserResponse> GetUserByIdAsync(Guid id);
}

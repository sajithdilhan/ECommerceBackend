using UserApi.Dtos;

namespace UserApi.Services;

public interface IUsersService
{
    public Task<UserResponse> CreateUserAsync(UserCreationRequest newUser, CancellationToken cts);
    public Task<UserResponse> GetUserByIdAsync(Guid id, CancellationToken cts);
}

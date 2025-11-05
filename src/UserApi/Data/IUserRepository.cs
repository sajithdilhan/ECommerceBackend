using Shared.Models;

namespace UserApi.Data
{
    public interface IUserRepository 
    {
        Task<User?> GetUserByIdAsync(Guid id);
        Task<User?> GetUserByEmailAsync(string email);
        Task<User?> CreateUserAsync(User newUser);
    }
}

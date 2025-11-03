using Shared.Models;

namespace UserService.Data
{
    public interface IUserRepository 
    {
        Task<User?> GetUserByIdAsync(Guid id);
        Task<User?> GetUserByEmailAsync(string email);
        Task<User> CreateUserAsync(User newUser);
    }
}

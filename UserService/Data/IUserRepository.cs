using Shared.Models;

namespace UserService.Data
{
    public interface IUserRepository 
    {
        Task<User?> GetUserByIdAsync(Guid id);
    }
}

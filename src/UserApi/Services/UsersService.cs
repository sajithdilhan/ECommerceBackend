using Shared.Contracts;
using Shared.Exceptions;
using UserApi.Data;
using UserApi.Dtos;

namespace UserApi.Services;

public class UsersService(IUserRepository userRepository, ILogger<UsersService> logger, IKafkaProducerWrapper producer) : IUsersService
{
    public async Task<UserResponse> CreateUserAsync(UserCreationRequest newUser)
    {
        try
        {
            var user = newUser.MapToUser();

            var exsistingUser = await userRepository.GetUserByEmailAsync(user.Email);
            if (exsistingUser != null)
            {
                logger.LogWarning("Conflict occurred while creating user with Email: {UserEmail}", newUser.Email);
                throw new ResourceConflictException($"User with email {user.Email} already exists.");
            }

            var createdUser = await userRepository.CreateUserAsync(user) ?? throw new Exception("Failed to create user.");

            await producer.ProduceAsync(createdUser.Id,
                new UserCreatedEvent { UserId = createdUser.Id, Email = createdUser.Email, Name = createdUser.Name });

            return UserResponse.MapUserToResponseDto(createdUser);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while creating user");
            throw;
        }
        
    }

    public async Task<UserResponse> GetUserByIdAsync(Guid id)
    {
        try
        {
            var response = await userRepository.GetUserByIdAsync(id);
            return response == null ? throw new NotFoundException($"User with ID {id} not found.")
                                    : UserResponse.MapUserToResponseDto(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while retrieving user with ID: {UserId}", id);
            throw;
        }
    }
}
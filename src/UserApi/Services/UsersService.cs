using Shared.Contracts;
using Shared.Exceptions;
using UserApi.Data;
using UserApi.Dtos;

namespace UserApi.Services;

public class UsersService : IUsersService
{
    private readonly IUserRepository _userRepository;
    private readonly IKafkaProducerWrapper _producer;
    private readonly ILogger<UsersService> _logger;

    public UsersService(IUserRepository userRepository, IKafkaProducerWrapper producer, ILogger<UsersService> logger)
    {
        _userRepository = userRepository;
        _producer = producer;
        _logger = logger;
    }

    public async Task<UserResponse> CreateUserAsync(UserCreationRequest newUser)
    {
        try
        {
            var user = newUser.MapToUser();

            var exsistingUser = await _userRepository.GetUserByEmailAsync(user.Email);
            if (exsistingUser != null)
            {
                _logger.LogWarning("Conflict occurred while creating user with Email: {UserEmail}", newUser.Email);
                throw new ResourceConflictException($"User with email {user.Email} already exists.");
            }

            var createdUser = await _userRepository.CreateUserAsync(user) ?? throw new Exception("Failed to create user.");

            //await _producer.ProduceAsync(createdUser.Id,
            //    new UserCreatedEvent { UserId = createdUser.Id, Email = createdUser.Email, Name = createdUser.Name });

            return UserResponse.MapUserToResponseDto(createdUser);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while creating user");
            throw;
        }
        
    }

    public async Task<UserResponse> GetUserByIdAsync(Guid id)
    {
        try
        {
            var response = await _userRepository.GetUserByIdAsync(id);
            return response == null ? throw new NotFoundException($"User with ID {id} not found.")
                                    : UserResponse.MapUserToResponseDto(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving user with ID: {UserId}", id);
            throw;
        }
    }
}

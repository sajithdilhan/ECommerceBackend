using Shared.Contracts;
using Shared.Exceptions;
using UserService.Data;
using UserService.Dtos;

namespace UserService.Services;

public class UsersService : IUsersService
{
    private readonly IUserRepository _userRepository;
    private readonly IKafkaProducerWrapper _producer;
    public UsersService(IUserRepository userRepository, IKafkaProducerWrapper producer)
    {
        _userRepository = userRepository;
        _producer = producer;
    }

    public async Task<UserResponse> CreateUserAsync(UserCreationRequest newUser)
    {
        var user = newUser.MapToUser();

        var exsistingUser = await _userRepository.GetUserByEmailAsync(user.Email);
        if (exsistingUser != null)
        {
            throw new ResourceConflictException($"User with email {user.Email} already exists.");
        }

        var createdUser = await _userRepository.CreateUserAsync(user).ContinueWith(task => UserResponse.MapUserToResponseDto(task.Result));

        await _producer.ProduceAsync(createdUser.Id, new UserCreatedEvent { UserId = createdUser.Id, Email = createdUser.Email, Name = createdUser.Name });
        return createdUser;
    }

    public async Task<UserResponse> GetUserByIdAsync(Guid id)
    {
        var response = await _userRepository.GetUserByIdAsync(id);
        return response == null ? throw new NotFoundException($"User with ID {id} not found.") : UserResponse.MapUserToResponseDto(response);
    }
}

using Shared.Contracts;

namespace OrderService.Events;

public class UserConsumerService : KafkaConsumerBase<UserCreatedEvent>
{
    private readonly ILogger<UserConsumerService> _logger;
    private readonly IConfiguration _config;

    public UserConsumerService(ILogger<UserConsumerService> logger, IConfiguration config)
        : base(logger, config)
    {
        _logger = logger;
        _config = config;
    }

    protected override string Topic => _config["Kafka:ConsumerTopic"] ?? string.Empty;

    protected override Task HandleMessageAsync(UserCreatedEvent @event)
    {
        _logger.LogInformation("Processed UserCreated: {UserId}, {Name}", @event.UserId, @event.Name);
        return Task.CompletedTask;
    }
}

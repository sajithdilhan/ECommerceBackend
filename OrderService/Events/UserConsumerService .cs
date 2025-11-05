using OrderService.Services;
using Shared.Contracts;

namespace OrderService.Events;

public class UserConsumerService : KafkaConsumerBase<UserCreatedEvent>
{
    private readonly ILogger<UserConsumerService> _logger;
    private readonly IConfiguration _config;
    private readonly IServiceProvider _serviceProvider;

    public UserConsumerService(ILogger<UserConsumerService> logger, IConfiguration config, IServiceProvider serviceProvider)
        : base(logger, config)
    {
        _logger = logger;
        _config = config;
        _serviceProvider = serviceProvider;
    }

    public override string Topic => _config["Kafka:ConsumerTopic"] ?? string.Empty;

    public override async Task HandleMessageAsync(UserCreatedEvent @event)
    {
        using var scope = _serviceProvider.CreateScope();
        var ordersService = scope.ServiceProvider.GetRequiredService<IOrdersService>();
        await ordersService.CreateKnownUserAsync(new Shared.Models.KnownUser() { UserId = @event.UserId, Email = @event.Email });
        _logger.LogInformation("Processed UserCreated: {UserId}, {Name}", @event.UserId, @event.Name);
    }
}
using Shared.Contracts;

namespace UserService.Events;

public class OrderConsumerService : KafkaConsumerBase<OrderCreatedEvent>
{
    private readonly ILogger<OrderConsumerService> _logger;
    private readonly IConfiguration _config;

    public OrderConsumerService(ILogger<OrderConsumerService> logger, IConfiguration config)
        : base(logger, config)
    {
        _logger = logger;
        _config = config;
    }

    public override string Topic => _config["Kafka:ConsumerTopic"] ?? string.Empty;

    public override Task HandleMessageAsync(OrderCreatedEvent @event)
    {
        _logger.LogInformation("Processed OrderCreated: {OrderId}, {Product}", @event.Id, @event.Product);
        return Task.CompletedTask;
    }
}

using Shared.Contracts;

namespace OrderService.Events;

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

    protected override string Topic => _config["Kafka:ConsumerTopic"] ?? string.Empty;

    protected override Task HandleMessageAsync(OrderCreatedEvent @event)
    {
        _logger.LogInformation("Processed OrderCreated: {OrderId}, {Product}", @event.Id, @event.Product);
        return Task.CompletedTask;
    }
}

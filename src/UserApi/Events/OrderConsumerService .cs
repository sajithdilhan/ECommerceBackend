using Shared.Contracts;

namespace UserApi.Events;

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

    public override async Task HandleMessageAsync(OrderCreatedEvent? eventMessage)
    {
        if (eventMessage == null) return;

        try
        {
            _logger.LogInformation("Processed OrderCreated: {OrderId}, {Product}", eventMessage.Id, eventMessage.Product);
            // Possible to keep records of orders associated with users in User Service database, but for simplicity, just log the event here.
        }
        catch (Exception)
        {
            _logger.LogError("Error processing OrderCreated: {OrderId}", eventMessage.Id);
            throw;
        }
    }
}
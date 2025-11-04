using Confluent.Kafka;
using Shared.Contracts;
using System.Text.Json;

public class KafkaProducerService
{
    private readonly IProducer<string, string> _producer;
    private const string Topic = "user-created";
    private readonly ILogger<KafkaProducerService> _logger;

    public KafkaProducerService(IConfiguration configuration, ILogger<KafkaProducerService> logger)
    {
        _logger = logger;
        var config = new ProducerConfig
        {
            BootstrapServers = configuration["Kafka:BootstrapServers"]
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task PublishUserCreatedAsync(Guid userId, string name, string email)
    {
        var userEvent = new UserCreatedEvent
        {
            UserId = userId,
            Name = name,
            Email = email
        };

        var message = new Message<string, string>
        {
            Key = userId.ToString(),
            Value = JsonSerializer.Serialize(userEvent)
        };

        await _producer.ProduceAsync(Topic, message);
        _logger.LogInformation($"✅ Published UserCreated event: {userId}");
    }
}

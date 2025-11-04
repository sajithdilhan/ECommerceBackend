using Confluent.Kafka;
using System.Text.Json;

public class KafkaConsumerService : BackgroundService
{
    private readonly ILogger<KafkaConsumerService> _logger;
    private readonly IConfiguration _config;

    public KafkaConsumerService(ILogger<KafkaConsumerService> logger, IConfiguration config)
    {
        _logger = logger;
        _config = config;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Run(() =>
        {
            var consumerConfig = new ConsumerConfig
            {
                BootstrapServers = _config["Kafka:BootstrapServers"],
                GroupId = "order-service",
                AutoOffsetReset = AutoOffsetReset.Earliest
            };

            using var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
            consumer.Subscribe("user-created");

            _logger.LogInformation("🟢 OrderService listening to user-created topic...");

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    var cr = consumer.Consume(stoppingToken);
                    var userEvent = JsonSerializer.Deserialize<UserCreatedEvent>(cr.Message.Value);
                    _logger.LogInformation($"📩 Received UserCreated: {userEvent?.UserId}, {userEvent?.Name}");
                    // Do something (e.g., create welcome order)
                }
            }
            catch (OperationCanceledException)
            {
                consumer.Close();
            }
        }, stoppingToken);
    }
}

public record UserCreatedEvent(Guid UserId, string Name, string Email);

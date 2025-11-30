using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Shared.Contracts;

public class KafkaProducerWrapper : IKafkaProducerWrapper
{
    private readonly IProducer<string, string> _producer;
    private readonly IConfiguration _config;
    private readonly string _producerTopic;
    private readonly ILogger<KafkaProducerWrapper> _logger;

    public KafkaProducerWrapper(IConfiguration configuration, ILogger<KafkaProducerWrapper> logger)
    {
        _logger = logger;
        _config = configuration;
        _producerTopic = configuration["Kafka:ProducerTopic"] ?? string.Empty;
        var config = new ProducerConfig
        {
            BootstrapServers = _config["Kafka:BootstrapServers"]
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task ProduceAsync<T>(Guid key, T eventObject) where T : class
    {
        var message = new Message<string, string>
        {
            Key = key.ToString(),
            Value = JsonSerializer.Serialize(eventObject)
        };

        _logger.LogInformation("Producing message to topic {Topic} with key {Key} and value {Value}",
                                _producerTopic, message.Key, message.Value);
        CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await _producer.ProduceAsync(_producerTopic, message, cancellationToken: cts.Token);
    }
}
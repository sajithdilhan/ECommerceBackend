using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Shared.Contracts;

public abstract class KafkaConsumerBase<T> : BackgroundService
{
    private readonly ILogger _logger;
    private readonly IConfiguration _config;

    protected KafkaConsumerBase(ILogger logger, IConfiguration config)
    {
        _logger = logger;
        _config = config;
    }

    protected abstract string Topic { get; }

    protected abstract Task HandleMessageAsync(T @event);

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Run(() =>
        {
            var consumerConfig = new ConsumerConfig
            {
                BootstrapServers = _config["Kafka:BootstrapServers"],
                GroupId = _config["Kafka:GroupId"],
                AutoOffsetReset = AutoOffsetReset.Earliest
            };

            using var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
            consumer.Subscribe(Topic);

            _logger.LogInformation("Listening to topic {Topic}", Topic);

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    var cr = consumer.Consume(stoppingToken);
                    var @event = JsonSerializer.Deserialize<T>(cr.Message.Value);
                    HandleMessageAsync(@event).GetAwaiter().GetResult();
                }
            }
            catch (OperationCanceledException)
            {
                consumer.Close();
            }
        }, stoppingToken);
    }
}
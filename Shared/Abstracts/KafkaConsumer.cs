using Confluent.Kafka;
using Shared.Contracts;
using System.Text.Json;

namespace Shared.Abstracts;

public abstract class KafkaConsumer
{
    private readonly string _topic = string.Empty;
    private readonly string _groupId = string.Empty;
    private readonly string _botstrapServers = "localhost:9092";

    public KafkaConsumer(string topic, string groupId, string botstrapServers)
    {
        _topic = topic;
        _groupId = groupId;
        _botstrapServers = botstrapServers;
    }

    public virtual void StartConsuming()
    {
        try
        {
            var config = new ConsumerConfig
            {
                BootstrapServers = _botstrapServers,
                GroupId = _groupId,
                AutoOffsetReset = AutoOffsetReset.Earliest
            };

            using var consumer = new ConsumerBuilder<string, string>(config).Build();
            consumer.Subscribe(_topic);

            while (true)
            {
                var cr = consumer.Consume();
                var orderEvent = JsonSerializer.Deserialize<OrderCreatedEvent>(cr.Message.Value);
            }
        }
        catch (Exception)
        {
            throw;
        }
    }
}
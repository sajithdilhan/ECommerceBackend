using Confluent.Kafka;

namespace Shared.Abstracts;

public abstract class KafkaProducer
{
    public readonly IProducer<string, string> _producer;
    public readonly string _topic = string.Empty;
    public readonly string _botstrapServers = "localhost:9092";

    public KafkaProducer(string topic, string botstrapServers)
    {
        _topic = topic;
        _botstrapServers = botstrapServers;
        var config = new ProducerConfig { BootstrapServers = _botstrapServers };
        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public abstract Task PublishCreatedAsync<T>(T entity);
}
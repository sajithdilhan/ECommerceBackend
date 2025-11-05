namespace Shared.Contracts;

public interface IKafkaProducerWrapper
{
    Task ProduceAsync<T>(Guid key, T eventObject) where T : class;
}

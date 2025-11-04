namespace Shared.Contracts;

public interface IKafkaProducerWrapper
{
    Task ProduceAsync<TEvent>(Guid key, TEvent eventObject) where TEvent : class;
}

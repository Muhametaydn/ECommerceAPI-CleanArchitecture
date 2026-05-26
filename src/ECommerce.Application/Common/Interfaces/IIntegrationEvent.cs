namespace ECommerce.Application.Common.Interfaces
{
    /// <summary>
    /// Integration event marker arayüzü.
    /// Integration eventler bounded context'ler arası iletişimi temsil eder.
    /// OutboxMessage.Payload alanında JSON olarak saklanır; Hangfire job'u RabbitMQ'ya iletir.
    /// </summary>
    public interface IIntegrationEvent
    {
        Guid EventId { get; }
        DateTime OccurredOn { get; }
        string EventType { get; }
    }
}

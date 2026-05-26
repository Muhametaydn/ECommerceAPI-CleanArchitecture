namespace ECommerce.Application.Common.Interfaces
{
    /// <summary>
    /// Event bus soyutlaması.
    /// Şu an InMemoryEventBus ile implemente edilmiştir.
    /// Faz 4b'de RabbitMQ + MassTransit implementasyonu eklenecek.
    /// </summary>
    public interface IEventBus
    {
        /// <summary>Integration event'i yayımlar (publish eder)</summary>
        Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken = default)
            where TEvent : IIntegrationEvent;
    }
}

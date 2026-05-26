using ECommerce.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.Events
{
    /// <summary>
    /// IEventBus'ın in-memory stub implementasyonu.
    /// Faz 4b'de bu sınıf RabbitMQ + MassTransit implementasyonuyla değiştirilecek.
    /// Şu an sadece loglama yaparak event'i simüle eder.
    /// </summary>
    public sealed class InMemoryEventBus : IEventBus
    {
        private readonly ILogger<InMemoryEventBus> _logger;

        public InMemoryEventBus(ILogger<InMemoryEventBus> logger)
        {
            _logger = logger;
        }

        public Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken = default)
            where TEvent : IIntegrationEvent
        {
            _logger.LogInformation(
                "[InMemoryEventBus] Integration event yayımlandı: {EventType} | EventId: {EventId} | OccurredOn: {OccurredOn}",
                integrationEvent.EventType,
                integrationEvent.EventId,
                integrationEvent.OccurredOn);

            // TODO (Faz 4b): RabbitMQ/MassTransit ile gerçek publish
            return Task.CompletedTask;
        }
    }
}

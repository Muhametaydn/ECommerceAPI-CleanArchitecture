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
            where TEvent : class
        {
            if (integrationEvent is IIntegrationEvent described)
                _logger.LogInformation(
                    "[InMemoryEventBus] Integration event yayımlandı: {EventType} | EventId: {EventId} | OccurredOn: {OccurredOn}",
                    described.EventType,
                    described.EventId,
                    described.OccurredOn);
            else
                _logger.LogInformation("[InMemoryEventBus] Olay yayımlandı: {EventType}", typeof(TEvent).Name);

            // TODO (Faz 4b): RabbitMQ/MassTransit ile gerçek publish
            return Task.CompletedTask;
        }
    }
}

using ECommerce.Application.Common.Interfaces;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.EventBus
{
    /// <summary>
    /// IEventBus'ın MassTransit + RabbitMQ implementasyonu.
    /// InMemoryEventBus stub'ının üretim versiyonu.
    /// </summary>
    public sealed class MassTransitEventBus : IEventBus
    {
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly ILogger<MassTransitEventBus> _logger;

        public MassTransitEventBus(IPublishEndpoint publishEndpoint, ILogger<MassTransitEventBus> logger)
        {
            _publishEndpoint = publishEndpoint;
            _logger = logger;
        }

        public async Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken = default)
            where TEvent : IIntegrationEvent
        {
            _logger.LogInformation(
                "[MassTransitEventBus] Yayımlanıyor: {EventType} | EventId: {EventId}",
                integrationEvent.EventType,
                integrationEvent.EventId);

            await _publishEndpoint.Publish(integrationEvent, cancellationToken);
        }
    }
}

using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Notifications;
using ECommerce.Application.IntegrationEvents;
using ECommerce.Domain.Events.Orders;
using ECommerce.Domain.Outbox;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ECommerce.Application.Features.Orders.EventHandlers
{
    /// <summary>
    /// OrderCreatedDomainEvent handler'ı.
    /// Outbox tablosuna integration event mesajı yazar.
    /// </summary>
    public sealed class OrderCreatedDomainEventHandler
        : INotificationHandler<DomainEventNotification<OrderCreatedDomainEvent>>
    {
        private readonly ILogger<OrderCreatedDomainEventHandler> _logger;
        private readonly IOutboxRepository _outboxRepository;

        public OrderCreatedDomainEventHandler(
            ILogger<OrderCreatedDomainEventHandler> logger,
            IOutboxRepository outboxRepository)
        {
            _logger = logger;
            _outboxRepository = outboxRepository;
        }

        public async Task Handle(
            DomainEventNotification<OrderCreatedDomainEvent> notification,
            CancellationToken cancellationToken)
        {
            var domainEvent = notification.DomainEvent;

            _logger.LogInformation(
                "Sipariş oluşturuldu | OrderId: {OrderId} | OrderNumber: {OrderNumber} | UserId: {UserId}",
                domainEvent.OrderId, domainEvent.OrderNumber, domainEvent.UserId);

            // Integration event oluştur ve outbox'a yaz
            var integrationEvent = new OrderCreatedIntegrationEvent(
                domainEvent.OrderId,
                domainEvent.OrderNumber,
                domainEvent.UserId,
                domainEvent.TotalAmount);

            var outboxMessage = new OutboxMessage
            {
                Id = Guid.NewGuid(),
                Type = typeof(OrderCreatedIntegrationEvent).FullName!,
                Payload = JsonSerializer.Serialize(integrationEvent),
                CreatedAt = DateTime.UtcNow
            };

            await _outboxRepository.AddAsync(outboxMessage, cancellationToken);
        }
    }
}

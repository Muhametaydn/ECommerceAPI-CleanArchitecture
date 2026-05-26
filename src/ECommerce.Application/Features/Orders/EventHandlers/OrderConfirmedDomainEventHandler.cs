using ECommerce.Application.Common.Notifications;
using ECommerce.Application.IntegrationEvents;
using ECommerce.Domain.Events.Orders;
using ECommerce.Domain.Outbox;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using ECommerce.Application.Common.Interfaces;

namespace ECommerce.Application.Features.Orders.EventHandlers
{
    public sealed class OrderConfirmedDomainEventHandler
        : INotificationHandler<DomainEventNotification<OrderConfirmedDomainEvent>>
    {
        private readonly ILogger<OrderConfirmedDomainEventHandler> _logger;
        private readonly IOutboxRepository _outboxRepository;

        public OrderConfirmedDomainEventHandler(
            ILogger<OrderConfirmedDomainEventHandler> logger,
            IOutboxRepository outboxRepository)
        {
            _logger = logger;
            _outboxRepository = outboxRepository;
        }

        public async Task Handle(
            DomainEventNotification<OrderConfirmedDomainEvent> notification,
            CancellationToken cancellationToken)
        {
            var domainEvent = notification.DomainEvent;

            _logger.LogInformation(
                "Sipariş onaylandı | OrderId: {OrderId} | OrderNumber: {OrderNumber}",
                domainEvent.OrderId, domainEvent.OrderNumber);

            var integrationEvent = new OrderStatusChangedIntegrationEvent(
                domainEvent.OrderId,
                domainEvent.OrderNumber,
                domainEvent.UserId,
                "Confirmed");

            var outboxMessage = new OutboxMessage
            {
                Id = Guid.NewGuid(),
                Type = typeof(OrderStatusChangedIntegrationEvent).FullName!,
                Payload = JsonSerializer.Serialize(integrationEvent),
                CreatedAt = DateTime.UtcNow
            };

            await _outboxRepository.AddAsync(outboxMessage, cancellationToken);
        }
    }
}

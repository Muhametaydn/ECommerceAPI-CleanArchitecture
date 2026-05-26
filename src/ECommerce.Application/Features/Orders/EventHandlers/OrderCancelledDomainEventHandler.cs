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
    public sealed class OrderCancelledDomainEventHandler
        : INotificationHandler<DomainEventNotification<OrderCancelledDomainEvent>>
    {
        private readonly ILogger<OrderCancelledDomainEventHandler> _logger;
        private readonly IOutboxRepository _outboxRepository;

        public OrderCancelledDomainEventHandler(
            ILogger<OrderCancelledDomainEventHandler> logger,
            IOutboxRepository outboxRepository)
        {
            _logger = logger;
            _outboxRepository = outboxRepository;
        }

        public async Task Handle(
            DomainEventNotification<OrderCancelledDomainEvent> notification,
            CancellationToken cancellationToken)
        {
            var domainEvent = notification.DomainEvent;

            _logger.LogInformation(
                "Sipariş iptal edildi | OrderId: {OrderId} | OrderNumber: {OrderNumber}",
                domainEvent.OrderId, domainEvent.OrderNumber);

            var integrationEvent = new OrderStatusChangedIntegrationEvent(
                domainEvent.OrderId,
                domainEvent.OrderNumber,
                domainEvent.UserId,
                "Cancelled");

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

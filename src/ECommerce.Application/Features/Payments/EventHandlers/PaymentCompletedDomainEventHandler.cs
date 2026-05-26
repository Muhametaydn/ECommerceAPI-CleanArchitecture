using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Notifications;
using ECommerce.Application.IntegrationEvents;
using ECommerce.Domain.Events.Payments;
using ECommerce.Domain.Outbox;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ECommerce.Application.Features.Payments.EventHandlers
{
    /// <summary>
    /// PaymentCompletedDomainEvent handler'ı.
    /// Ödeme tamamlandığında muhasebe ve fatura sistemleri için outbox mesajı yazar.
    /// </summary>
    public sealed class PaymentCompletedDomainEventHandler
        : INotificationHandler<DomainEventNotification<PaymentCompletedDomainEvent>>
    {
        private readonly ILogger<PaymentCompletedDomainEventHandler> _logger;
        private readonly IOutboxRepository _outboxRepository;

        public PaymentCompletedDomainEventHandler(
            ILogger<PaymentCompletedDomainEventHandler> logger,
            IOutboxRepository outboxRepository)
        {
            _logger = logger;
            _outboxRepository = outboxRepository;
        }

        public async Task Handle(
            DomainEventNotification<PaymentCompletedDomainEvent> notification,
            CancellationToken cancellationToken)
        {
            var domainEvent = notification.DomainEvent;

            _logger.LogInformation(
                "Ödeme tamamlandı | PaymentId: {PaymentId} | OrderId: {OrderId} | Amount: {Amount:C} | TransactionId: {TransactionId}",
                domainEvent.PaymentId, domainEvent.OrderId, domainEvent.Amount, domainEvent.TransactionId);

            var integrationEvent = new PaymentProcessedIntegrationEvent(
                domainEvent.PaymentId,
                domainEvent.OrderId,
                domainEvent.Amount,
                domainEvent.TransactionId);

            var outboxMessage = new OutboxMessage
            {
                Id = Guid.NewGuid(),
                Type = typeof(PaymentProcessedIntegrationEvent).FullName!,
                Payload = JsonSerializer.Serialize(integrationEvent),
                CreatedAt = DateTime.UtcNow
            };

            await _outboxRepository.AddAsync(outboxMessage, cancellationToken);
        }
    }
}

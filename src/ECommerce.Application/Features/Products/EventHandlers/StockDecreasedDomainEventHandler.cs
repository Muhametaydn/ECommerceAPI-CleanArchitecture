using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Notifications;
using ECommerce.Application.IntegrationEvents;
using ECommerce.Domain.Events.Products;
using ECommerce.Domain.Outbox;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ECommerce.Application.Features.Products.EventHandlers
{
    /// <summary>
    /// StockDecreasedDomainEvent handler'ı.
    /// Stok kritik seviyenin altına düştüğünde satın alma departmanı için outbox mesajı yazar.
    /// </summary>
    public sealed class StockDecreasedDomainEventHandler
        : INotificationHandler<DomainEventNotification<StockDecreasedDomainEvent>>
    {
        private readonly ILogger<StockDecreasedDomainEventHandler> _logger;
        private readonly IOutboxRepository _outboxRepository;

        public StockDecreasedDomainEventHandler(
            ILogger<StockDecreasedDomainEventHandler> logger,
            IOutboxRepository outboxRepository)
        {
            _logger = logger;
            _outboxRepository = outboxRepository;
        }

        public async Task Handle(
            DomainEventNotification<StockDecreasedDomainEvent> notification,
            CancellationToken cancellationToken)
        {
            var domainEvent = notification.DomainEvent;

            _logger.LogInformation(
                "Stok düştü | ProductId: {ProductId} | Önceki: {PreviousStock} → Yeni: {NewStock}",
                domainEvent.ProductId, domainEvent.PreviousStock, domainEvent.NewStock);

            // Yalnızca düşük stok eşiği aşıldığında integration event yayımla
            if (!domainEvent.IsLowStock) return;

            _logger.LogWarning(
                "DÜŞÜK STOK UYARISI | Ürün: {ProductName} | Mevcut: {NewStock} | Eşik: {Threshold}",
                domainEvent.ProductName, domainEvent.NewStock, StockDecreasedDomainEvent.LowStockThreshold);

            var integrationEvent = new LowStockAlertIntegrationEvent(
                domainEvent.ProductId,
                domainEvent.ProductName,
                domainEvent.NewStock,
                StockDecreasedDomainEvent.LowStockThreshold);

            var outboxMessage = new OutboxMessage
            {
                Id = Guid.NewGuid(),
                Type = typeof(LowStockAlertIntegrationEvent).FullName!,
                Payload = JsonSerializer.Serialize(integrationEvent),
                CreatedAt = DateTime.UtcNow
            };

            await _outboxRepository.AddAsync(outboxMessage, cancellationToken);
        }
    }
}

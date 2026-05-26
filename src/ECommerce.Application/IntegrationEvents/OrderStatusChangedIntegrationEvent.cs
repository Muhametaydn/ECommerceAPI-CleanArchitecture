using ECommerce.Application.Common.Interfaces;

namespace ECommerce.Application.IntegrationEvents
{
    /// <summary>
    /// Sipariş durumu değiştiğinde dışarıya yayılan integration event.
    /// Müşteri bildirimleri (SMS/e-posta), kargo takip sistemi entegrasyonu için kullanılır.
    /// </summary>
    public sealed class OrderStatusChangedIntegrationEvent : IIntegrationEvent
    {
        public Guid EventId { get; } = Guid.NewGuid();
        public DateTime OccurredOn { get; } = DateTime.UtcNow;
        public string EventType => nameof(OrderStatusChangedIntegrationEvent);

        public Guid OrderId { get; init; }
        public string OrderNumber { get; init; } = string.Empty;
        public Guid UserId { get; init; }
        public string NewStatus { get; init; } = string.Empty;

        public OrderStatusChangedIntegrationEvent() { }

        public OrderStatusChangedIntegrationEvent(Guid orderId, string orderNumber, Guid userId, string newStatus)
        {
            OrderId = orderId;
            OrderNumber = orderNumber;
            UserId = userId;
            NewStatus = newStatus;
        }
    }
}

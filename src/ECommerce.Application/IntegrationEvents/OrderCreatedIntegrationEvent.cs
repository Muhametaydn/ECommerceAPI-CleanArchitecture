using ECommerce.Application.Common.Interfaces;

namespace ECommerce.Application.IntegrationEvents
{
    /// <summary>
    /// Sipariş oluşturulduğunda dışarıya yayılan integration event.
    /// E-posta bildirimi, analitik, vs. için kullanılır.
    /// </summary>
    public sealed class OrderCreatedIntegrationEvent : IIntegrationEvent
    {
        public Guid EventId { get; } = Guid.NewGuid();
        public DateTime OccurredOn { get; } = DateTime.UtcNow;
        public string EventType => nameof(OrderCreatedIntegrationEvent);

        public Guid OrderId { get; init; }
        public string OrderNumber { get; init; } = string.Empty;
        public Guid UserId { get; init; }
        public decimal TotalAmount { get; init; }

        public OrderCreatedIntegrationEvent() { }

        public OrderCreatedIntegrationEvent(Guid orderId, string orderNumber, Guid userId, decimal totalAmount)
        {
            OrderId = orderId;
            OrderNumber = orderNumber;
            UserId = userId;
            TotalAmount = totalAmount;
        }
    }
}

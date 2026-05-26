using ECommerce.Domain.Common;

namespace ECommerce.Domain.Events.Orders
{
    /// <summary>Sipariş onaylandığında yayılır (Pending → Confirmed).</summary>
    public sealed class OrderConfirmedDomainEvent : IDomainEvent
    {
        public Guid EventId { get; } = Guid.NewGuid();
        public DateTime OccurredOn { get; } = DateTime.UtcNow;

        public Guid OrderId { get; }
        public string OrderNumber { get; }
        public Guid UserId { get; }

        public OrderConfirmedDomainEvent(Guid orderId, string orderNumber, Guid userId)
        {
            OrderId = orderId;
            OrderNumber = orderNumber;
            UserId = userId;
        }
    }
}

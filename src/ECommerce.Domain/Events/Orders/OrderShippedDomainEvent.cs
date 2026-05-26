using ECommerce.Domain.Common;

namespace ECommerce.Domain.Events.Orders
{
    /// <summary>Sipariş kargoya verildiğinde yayılır (Confirmed → Shipped).</summary>
    public sealed class OrderShippedDomainEvent : IDomainEvent
    {
        public Guid EventId { get; } = Guid.NewGuid();
        public DateTime OccurredOn { get; } = DateTime.UtcNow;

        public Guid OrderId { get; }
        public string OrderNumber { get; }
        public Guid UserId { get; }

        public OrderShippedDomainEvent(Guid orderId, string orderNumber, Guid userId)
        {
            OrderId = orderId;
            OrderNumber = orderNumber;
            UserId = userId;
        }
    }
}

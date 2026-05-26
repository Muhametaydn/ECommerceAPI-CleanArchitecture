using ECommerce.Domain.Common;

namespace ECommerce.Domain.Events.Orders
{
    /// <summary>Sipariş iptal edildiğinde yayılır.</summary>
    public sealed class OrderCancelledDomainEvent : IDomainEvent
    {
        public Guid EventId { get; } = Guid.NewGuid();
        public DateTime OccurredOn { get; } = DateTime.UtcNow;

        public Guid OrderId { get; }
        public string OrderNumber { get; }
        public Guid UserId { get; }

        public OrderCancelledDomainEvent(Guid orderId, string orderNumber, Guid userId)
        {
            OrderId = orderId;
            OrderNumber = orderNumber;
            UserId = userId;
        }
    }
}

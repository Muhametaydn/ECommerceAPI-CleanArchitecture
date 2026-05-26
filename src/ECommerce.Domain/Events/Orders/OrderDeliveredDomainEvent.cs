using ECommerce.Domain.Common;

namespace ECommerce.Domain.Events.Orders
{
    /// <summary>Sipariş teslim edildiğinde yayılır (Shipped → Delivered).</summary>
    public sealed class OrderDeliveredDomainEvent : IDomainEvent
    {
        public Guid EventId { get; } = Guid.NewGuid();
        public DateTime OccurredOn { get; } = DateTime.UtcNow;

        public Guid OrderId { get; }
        public string OrderNumber { get; }
        public Guid UserId { get; }

        public OrderDeliveredDomainEvent(Guid orderId, string orderNumber, Guid userId)
        {
            OrderId = orderId;
            OrderNumber = orderNumber;
            UserId = userId;
        }
    }
}

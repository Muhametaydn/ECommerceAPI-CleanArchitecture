using ECommerce.Domain.Common;

namespace ECommerce.Domain.Events.Orders
{
    /// <summary>Sipariş ilk oluşturulduğunda yayılır.</summary>
    public sealed class OrderCreatedDomainEvent : IDomainEvent
    {
        public Guid EventId { get; } = Guid.NewGuid();
        public DateTime OccurredOn { get; } = DateTime.UtcNow;

        public Guid OrderId { get; }
        public string OrderNumber { get; }
        public Guid UserId { get; }
        public decimal TotalAmount { get; }

        public OrderCreatedDomainEvent(Guid orderId, string orderNumber, Guid userId, decimal totalAmount)
        {
            OrderId = orderId;
            OrderNumber = orderNumber;
            UserId = userId;
            TotalAmount = totalAmount;
        }
    }
}

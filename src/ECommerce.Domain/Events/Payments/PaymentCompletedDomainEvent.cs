using ECommerce.Domain.Common;

namespace ECommerce.Domain.Events.Payments
{
    /// <summary>Ödeme başarıyla tamamlandığında yayılır.</summary>
    public sealed class PaymentCompletedDomainEvent : IDomainEvent
    {
        public Guid EventId { get; } = Guid.NewGuid();
        public DateTime OccurredOn { get; } = DateTime.UtcNow;

        public Guid PaymentId { get; }
        public Guid OrderId { get; }
        public decimal Amount { get; }
        public string TransactionId { get; }

        public PaymentCompletedDomainEvent(Guid paymentId, Guid orderId, decimal amount, string transactionId)
        {
            PaymentId = paymentId;
            OrderId = orderId;
            Amount = amount;
            TransactionId = transactionId;
        }
    }
}

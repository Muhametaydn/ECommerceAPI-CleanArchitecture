using ECommerce.Domain.Events.Payments;

namespace ECommerce.Domain.Entities
{
    public class Payment : Common.BaseEntity
    {
        public decimal Amount { get; set; }
        public Enums.PaymentMethod Method { get; set; }
        public Enums.PaymentStatus Status { get; set; } = Enums.PaymentStatus.Pending;
        public string? TransacionId { get; set; }

        public Guid OrderId { get; set; }
        public Order Order { get; set; } = null!;

        /// <summary>Ödemeyi tamamlar ve domain event yayar.</summary>
        public void MarkAsCompleted(string transactionId)
        {
            if (Status != Enums.PaymentStatus.Pending)
                throw new InvalidOperationException("Sadece bekleyen ödemeler tamamlanabilir.");

            Status = Enums.PaymentStatus.Completed;
            TransacionId = transactionId;
            UpdateAt = DateTime.UtcNow;

            AddDomainEvent(new PaymentCompletedDomainEvent(Id, OrderId, Amount, transactionId));
        }

        public void MarkAsFailed()
        {
            Status = Enums.PaymentStatus.Failed;
            UpdateAt = DateTime.UtcNow;
        }
    }
}

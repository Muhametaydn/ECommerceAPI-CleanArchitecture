using ECommerce.Application.Common.Interfaces;

namespace ECommerce.Application.IntegrationEvents
{
    /// <summary>
    /// Ödeme tamamlandığında dışarıya yayılan integration event.
    /// Muhasebe sistemi, fatura oluşturma, sipariş onaylama akışı için kullanılır.
    /// </summary>
    public sealed class PaymentProcessedIntegrationEvent : IIntegrationEvent
    {
        public Guid EventId { get; } = Guid.NewGuid();
        public DateTime OccurredOn { get; } = DateTime.UtcNow;
        public string EventType => nameof(PaymentProcessedIntegrationEvent);

        public Guid PaymentId { get; init; }
        public Guid OrderId { get; init; }
        public decimal Amount { get; init; }
        public string TransactionId { get; init; } = string.Empty;

        public PaymentProcessedIntegrationEvent() { }

        public PaymentProcessedIntegrationEvent(Guid paymentId, Guid orderId, decimal amount, string transactionId)
        {
            PaymentId = paymentId;
            OrderId = orderId;
            Amount = amount;
            TransactionId = transactionId;
        }
    }
}

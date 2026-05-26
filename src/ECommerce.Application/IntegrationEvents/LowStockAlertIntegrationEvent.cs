using ECommerce.Application.Common.Interfaces;

namespace ECommerce.Application.IntegrationEvents
{
    /// <summary>
    /// Ürün stoğu kritik seviyenin altına düştüğünde dışarıya yayılan integration event.
    /// Satın alma departmanı bildirimi, otomatik sipariş tetikleme için kullanılır.
    /// </summary>
    public sealed class LowStockAlertIntegrationEvent : IIntegrationEvent
    {
        public Guid EventId { get; } = Guid.NewGuid();
        public DateTime OccurredOn { get; } = DateTime.UtcNow;
        public string EventType => nameof(LowStockAlertIntegrationEvent);

        public Guid ProductId { get; init; }
        public string ProductName { get; init; } = string.Empty;
        public int CurrentStock { get; init; }
        public int Threshold { get; init; }

        public LowStockAlertIntegrationEvent() { }

        public LowStockAlertIntegrationEvent(Guid productId, string productName, int currentStock, int threshold)
        {
            ProductId = productId;
            ProductName = productName;
            CurrentStock = currentStock;
            Threshold = threshold;
        }
    }
}

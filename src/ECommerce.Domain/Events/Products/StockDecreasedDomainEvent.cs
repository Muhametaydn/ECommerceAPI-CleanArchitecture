using ECommerce.Domain.Common;

namespace ECommerce.Domain.Events.Products
{
    /// <summary>Ürün stoğu düştüğünde yayılır. Düşük stok uyarısı için kullanılır.</summary>
    public sealed class StockDecreasedDomainEvent : IDomainEvent
    {
        public Guid EventId { get; } = Guid.NewGuid();
        public DateTime OccurredOn { get; } = DateTime.UtcNow;

        public Guid ProductId { get; }
        public string ProductName { get; }
        public int PreviousStock { get; }
        public int NewStock { get; }
        public int DecreasedBy { get; }

        /// <summary>Düşük stok eşiği (varsayılan 5). Bu değerin altına düştüğünde uyarı tetiklenir.</summary>
        public const int LowStockThreshold = 5;

        public bool IsLowStock => NewStock <= LowStockThreshold;

        public StockDecreasedDomainEvent(Guid productId, string productName, int previousStock, int newStock, int decreasedBy)
        {
            ProductId = productId;
            ProductName = productName;
            PreviousStock = previousStock;
            NewStock = newStock;
            DecreasedBy = decreasedBy;
        }
    }
}

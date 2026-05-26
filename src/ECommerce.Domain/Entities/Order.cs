using ECommerce.Domain.Events.Orders;

namespace ECommerce.Domain.Entities
{
    public class Order : Common.BaseEntity
    {
        public string OrderNumber { get; set; } = string.Empty;
        public Enums.OrderStatus Status { get; set; } = Enums.OrderStatus.Pending;
        public decimal TotalAmount { get; private set; }
        public decimal DiscountAmount { get; set; }
        public string? CouponCode { get; set; }
        public string? Note { get; set; }

        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        public Guid ShippingAddressId { get; set; }
        public Address ShippingAddress { get; set; } = null!;

        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public Payment? Payment { get; set; }

        // ── Fabrika metodu (CreateOrderCommandHandler kullanır) ───────────────
        public static string GenerateOrderNumber()
            => $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}";

        /// <summary>
        /// Sipariş oluşturulduktan sonra CreateOrderCommandHandler tarafından çağrılır.
        /// Domain event burada raise edilir.
        /// </summary>
        public void RaiseOrderCreated()
        {
            AddDomainEvent(new OrderCreatedDomainEvent(Id, OrderNumber, UserId, TotalAmount));
        }

        // ── Toplam tutar hesapla ──────────────────────────────────────────────
        public void CalculateTotal()
        {
            var subTotal = OrderItems.Sum(item => item.TotalPrice);
            TotalAmount = subTotal - DiscountAmount;
            if (TotalAmount < 0) TotalAmount = 0;
        }

        // ── State Machine ─────────────────────────────────────────────────────

        /// <summary>Siparişi onaylar: Pending → Confirmed</summary>
        public void Confirm()
        {
            if (Status != Enums.OrderStatus.Pending)
                throw new InvalidOperationException("Sadece bekleyen siparişler onaylanabilir.");

            Status = Enums.OrderStatus.Confirmed;
            UpdateAt = DateTime.UtcNow;

            AddDomainEvent(new OrderConfirmedDomainEvent(Id, OrderNumber, UserId));
        }

        /// <summary>Siparişi kargoya verir: Confirmed → Shipped</summary>
        public void Ship()
        {
            if (Status != Enums.OrderStatus.Confirmed)
                throw new InvalidOperationException("Sadece onaylanmış siparişler kargoya verilebilir.");

            Status = Enums.OrderStatus.Shipped;
            UpdateAt = DateTime.UtcNow;

            AddDomainEvent(new OrderShippedDomainEvent(Id, OrderNumber, UserId));
        }

        /// <summary>Siparişi teslim edildi olarak işaretler: Shipped → Delivered</summary>
        public void Deliver()
        {
            if (Status != Enums.OrderStatus.Shipped)
                throw new InvalidOperationException("Sadece kargodaki siparişler teslim edilebilir.");

            Status = Enums.OrderStatus.Delivered;
            UpdateAt = DateTime.UtcNow;

            AddDomainEvent(new OrderDeliveredDomainEvent(Id, OrderNumber, UserId));
        }

        /// <summary>Siparişi iptal eder</summary>
        public void Cancel()
        {
            if (Status == Enums.OrderStatus.Delivered
                || Status == Enums.OrderStatus.Cancelled
                || Status == Enums.OrderStatus.Refunded)
                throw new InvalidOperationException(
                    "Teslim edilmiş, iptal edilmiş veya iade edilmiş sipariş iptal edilemez.");

            Status = Enums.OrderStatus.Cancelled;
            UpdateAt = DateTime.UtcNow;

            AddDomainEvent(new OrderCancelledDomainEvent(Id, OrderNumber, UserId));
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Domain.Entities
{
    public class OrderItem : Common.BaseEntity
    {
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice => Quantity * UnitPrice;
        public Enums.OrderItemStatus Status { get; private set; } = Enums.OrderItemStatus.Pending;

        public Guid OrderId { get; set; }
        public Order Order { get; set; } = null!;

        public Guid ProductId { get; set; }
        public Product Product { get; set; } = null!;

        public void StartPreparing()
        {
            if (Status != Enums.OrderItemStatus.Pending)
                throw new InvalidOperationException("Sadece bekleyen ürünler hazırlanmaya alınabilir.");

            Status = Enums.OrderItemStatus.Preparing;
            UpdateAt = DateTime.UtcNow;
        }

        public void Ship()
        {
            if (Status != Enums.OrderItemStatus.Preparing)
                throw new InvalidOperationException("Sadece hazırlanan ürünler kargoya verilebilir.");

            Status = Enums.OrderItemStatus.Shipped;
            UpdateAt = DateTime.UtcNow;
        }

        public void Deliver()
        {
            if (Status != Enums.OrderItemStatus.Shipped)
                throw new InvalidOperationException("Sadece kargodaki ürünler teslim edilebilir.");

            Status = Enums.OrderItemStatus.Delivered;
            UpdateAt = DateTime.UtcNow;
        }

        public void Cancel()
        {
            if (Status == Enums.OrderItemStatus.Delivered)
                throw new InvalidOperationException("Teslim edilmiş ürün iptal edilemez.");

            Status = Enums.OrderItemStatus.Cancelled;
            UpdateAt = DateTime.UtcNow;
        }
    }
}

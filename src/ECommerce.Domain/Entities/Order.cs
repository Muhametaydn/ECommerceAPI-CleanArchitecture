using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Domain.Entities
{
    public class Order : Common.BaseEntity
    {
        public string OrderNumber { get; set; } = string.Empty;
        public Enums.OrderStatus Status { get; set; } = Enums.OrderStatus.Pending;
        public decimal TotalAmount { get; private set; }
        public string? Note { get; set; }

        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        public Guid ShippingAddressId { get; set; }
        public Address ShippingAddress { get; set; } = null!;

        public ICollection<OrderItem> OrderItems = new List<OrderItem>();
        public Payment? Payment { get; set; }


        //siparis numarasi uretme
        public static string GenerateOrderNumber()
        {
            return $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}";
        }

        //Toplam tutar hesapla
        public void CalculateTotal() { 
        TotalAmount = OrderItems.Sum(item => item.TotalPrice);
        }
        //Siparis onayla
        public void Confirm() {
            if (Status != Enums.OrderStatus.Pending)
                throw new InvalidOperationException("Sadece bekleyen siparişler onaylanabilir.");

            Status = Enums.OrderStatus.Confirmed;
            UpdateAt = DateTime.UtcNow;
        }

        //kargoyas ver
        public void Ship() { 
            if(Status != Enums.OrderStatus.Confirmed)
                throw new InvalidOperationException("Sadece onaylanmış siparişler kargoya verilebilir.");

            Status = Enums.OrderStatus.Confirmed;
            UpdateAt = DateTime.UtcNow;
        }

        //teslim et
        public void Deliver() { 
        
            if(Status != Enums.OrderStatus.Shipped)
                 throw new InvalidOperationException("Sadece kargodaki siparişler teslim edilebilir.");

            Status = Enums.OrderStatus.Delivered;
            UpdateAt = DateTime.UtcNow;

        }


        //iptal et

        public void Cancel() {
            if(Status != Enums.OrderStatus.Delivered)
                throw new InvalidOperationException("Teslim edilmiş sipariş iptal edilemez.");

            Status = Enums.OrderStatus.Cancelled;
            UpdateAt = DateTime.UtcNow;
        }






    }
}

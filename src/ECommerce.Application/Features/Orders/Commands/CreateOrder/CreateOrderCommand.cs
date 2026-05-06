using ECommerce.Application.Features.Orders.DTOs;
using ECommerce.Domain.Enums;
using MediatR;

namespace ECommerce.Application.Features.Orders.Commands.CreateOrder
{
    /// <summary>
    /// Sepetten sipariş oluşturma komutu.
    /// CartId üzerinden sepet bilgileri alınır, stok kontrolü yapılır,
    /// sipariş + ödeme kaydı oluşturulur ve sepet temizlenir.
    /// </summary>
    public class CreateOrderCommand : IRequest<OrderDetailDTO>
    {
        /// <summary>Kullanıcı ID (JWT'den gelir)</summary>
        public Guid UserId { get; set; }

        /// <summary>Redis sepet anahtarı</summary>
        public string CartId { get; set; } = string.Empty;

        /// <summary>Teslimat adresi ID</summary>
        public Guid ShippingAddressId { get; set; }

        /// <summary>Ödeme yöntemi</summary>
        public PaymentMethod PaymentMethod { get; set; }

        /// <summary>Sipariş notu (opsiyonel)</summary>
        public string? Note { get; set; }

        /// <summary>Kupon kodu (opsiyonel)</summary>
        public string? CouponCode { get; set; }
    }
}

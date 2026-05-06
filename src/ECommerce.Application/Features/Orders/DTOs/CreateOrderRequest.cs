using ECommerce.Domain.Enums;

namespace ECommerce.Application.Features.Orders.DTOs
{
    /// <summary>
    /// Sipariş oluşturma isteği - sepetten sipariş oluşturulur
    /// </summary>
    public class CreateOrderRequest
    {
        public Guid ShippingAddressId { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public string? Note { get; set; }
        public string? CouponCode { get; set; }
    }
}

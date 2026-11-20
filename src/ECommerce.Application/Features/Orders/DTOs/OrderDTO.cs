using ECommerce.Domain.Enums;

namespace ECommerce.Application.Features.Orders.DTOs
{
    /// <summary>
    /// Sipariş listesi için özet DTO
    /// </summary>
    public class OrderDTO
    {
        public Guid Id { get; init; }
        public string OrderNumber { get; init; } = string.Empty;
        public OrderStatus Status { get; init; }
        public string StatusText => Status.ToString();
        public decimal TotalAmount { get; init; }
        public int TotalItems { get; init; }
        public DateTime CreatedAt { get; init; }
    }

    /// <summary>
    /// Sipariş detay DTO - tüm bilgileri içerir
    /// </summary>
    public class OrderDetailDTO
    {
        public Guid Id { get; init; }
        public string OrderNumber { get; init; } = string.Empty;
        public OrderStatus Status { get; init; }
        public string StatusText => Status.ToString();
        public decimal TotalAmount { get; init; }
        public decimal DiscountAmount { get; init; }
        public string? CouponCode { get; init; }
        public string? Note { get; init; }

        // Adres bilgileri
        public string ShippingAddress { get; init; } = string.Empty;
        public string ShippingCity { get; init; } = string.Empty;
        public string ShippingDistrict { get; init; } = string.Empty;

        // Ödeme bilgileri
        public PaymentStatus? PaymentStatus { get; init; }
        public PaymentMethod? PaymentMethod { get; init; }

        // Sipariş kalemleri
        public List<OrderItemDTO> Items { get; init; } = new();

        public DateTime CreatedAt { get; init; }
        public DateTime? UpdatedAt { get; init; }
    }

    /// <summary>
    /// Satıcı panelinde yalnızca ilgili satıcının ürünlerini gösteren sipariş özeti.
    /// </summary>
    public class SellerOrderDTO
    {
        public Guid Id { get; init; }
        public string OrderNumber { get; init; } = string.Empty;
        public OrderStatus Status { get; init; }
        public string StatusText => Status.ToString();
        public decimal SellerSubtotal { get; init; }
        public int SellerTotalItems { get; init; }
        public bool CanManageStatus { get; init; }
        public bool ContainsOtherSellers { get; init; }
        public string ShippingAddress { get; init; } = string.Empty;
        public string ShippingCity { get; init; } = string.Empty;
        public string ShippingDistrict { get; init; } = string.Empty;
        public IReadOnlyList<OrderItemDTO> Items { get; init; } = Array.Empty<OrderItemDTO>();
        public DateTime CreatedAt { get; init; }
    }
}

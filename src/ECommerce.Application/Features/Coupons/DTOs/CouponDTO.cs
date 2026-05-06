using ECommerce.Domain.Enums;

namespace ECommerce.Application.Features.Coupons.DTOs
{
    public class CouponDTO
    {
        public Guid Id { get; init; }
        public string Code { get; init; } = string.Empty;
        public DiscountType DiscountType { get; init; }
        public string DiscountTypeText => DiscountType.ToString();
        public decimal DiscountValue { get; init; }
        public decimal? MinimumOrderAmount { get; init; }
        public int MaxUsageCount { get; init; }
        public int CurrentUsageCount { get; init; }
        public int RemainingUsageCount => MaxUsageCount - CurrentUsageCount;
        public DateTime ExpiryDate { get; init; }
        public bool IsActive { get; init; }
        public bool IsValid { get; init; }
        public DateTime CreatedAt { get; init; }
    }

    /// <summary>
    /// Kupon doğrulama sonucu
    /// </summary>
    public class CouponValidationDTO
    {
        public bool IsValid { get; init; }
        public string? Message { get; init; }
        public decimal DiscountAmount { get; init; }
        public decimal FinalAmount { get; init; }
        public string Code { get; init; } = string.Empty;
        public DiscountType? DiscountType { get; init; }
        public decimal? DiscountValue { get; init; }
    }
}

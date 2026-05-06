using ECommerce.Domain.Enums;
using MediatR;

namespace ECommerce.Application.Features.Coupons.Commands.CreateCoupon
{
    public class CreateCouponCommand : IRequest<Guid>
    {
        public string Code { get; set; } = string.Empty;
        public DiscountType DiscountType { get; set; }
        public decimal DiscountValue { get; set; }
        public decimal? MinimumOrderAmount { get; set; }
        public int MaxUsageCount { get; set; }
        public DateTime ExpiryDate { get; set; }
    }
}

using ECommerce.Application.Features.Coupons.DTOs;
using ECommerce.Domain.Interfaces;
using MediatR;

namespace ECommerce.Application.Features.Coupons.Queries.ValidateCoupon
{
    public class ValidateCouponQueryHandler : IRequestHandler<ValidateCouponQuery, CouponValidationDTO>
    {
        private readonly IUnitOfWork _unitOfWork;

        public ValidateCouponQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<CouponValidationDTO> Handle(ValidateCouponQuery request, CancellationToken cancellationToken)
        {
            var coupon = await _unitOfWork.Coupon.GetByCodeAsync(request.Code.ToUpperInvariant());

            if (coupon == null)
            {
                return new CouponValidationDTO
                {
                    IsValid = false,
                    Message = "Kupon kodu bulunamadı.",
                    Code = request.Code
                };
            }

            if (!coupon.IsValid())
            {
                return new CouponValidationDTO
                {
                    IsValid = false,
                    Message = "Kupon geçersiz veya süresi dolmuş.",
                    Code = request.Code
                };
            }

            if (coupon.MinimumOrderAmount.HasValue && request.OrderTotal < coupon.MinimumOrderAmount.Value)
            {
                return new CouponValidationDTO
                {
                    IsValid = false,
                    Message = $"Bu kuponu kullanmak için minimum sipariş tutarı {coupon.MinimumOrderAmount.Value:N2} TL olmalıdır.",
                    Code = request.Code
                };
            }

            var discountAmount = coupon.CalculateDiscount(request.OrderTotal);

            return new CouponValidationDTO
            {
                IsValid = true,
                Message = "Kupon başarıyla uygulandı.",
                DiscountAmount = discountAmount,
                FinalAmount = request.OrderTotal - discountAmount,
                Code = coupon.Code,
                DiscountType = coupon.DiscountType,
                DiscountValue = coupon.DiscountValue
            };
        }
    }
}

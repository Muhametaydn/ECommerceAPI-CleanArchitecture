using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using MediatR;

namespace ECommerce.Application.Features.Coupons.Commands.CreateCoupon
{
    public class CreateCouponCommandHandler : IRequestHandler<CreateCouponCommand, Guid>
    {
        private readonly IUnitOfWork _unitOfWork;

        public CreateCouponCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Guid> Handle(CreateCouponCommand request, CancellationToken cancellationToken)
        {
            // Aynı kodla kupon var mı kontrol et
            var existing = await _unitOfWork.Coupon.GetByCodeAsync(request.Code.ToUpperInvariant());
            if (existing != null)
                throw new InvalidOperationException($"'{request.Code}' kodu zaten kullanılıyor.");

            var coupon = new Coupon
            {
                Id = Guid.NewGuid(),
                Code = request.Code.ToUpperInvariant(),
                DiscountType = request.DiscountType,
                DiscountValue = request.DiscountValue,
                MinimumOrderAmount = request.MinimumOrderAmount,
                MaxUsageCount = request.MaxUsageCount,
                CurrentUsageCount = 0,
                ExpiryDate = request.ExpiryDate,
                IsActive = true
            };

            await _unitOfWork.Coupon.AddAsync(coupon);
            await _unitOfWork.SaveChangesAsync();

            return coupon.Id;
        }
    }
}
